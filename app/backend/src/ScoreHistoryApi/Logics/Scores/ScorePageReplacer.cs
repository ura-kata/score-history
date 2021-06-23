using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScorePageReplacer
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;

        public ScorePageReplacer(IAmazonDynamoDB dynamoDbClient, IScoreQuota scoreQuota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _scoreQuota = scoreQuota;
            _configuration = configuration;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");
            ScoreItemRelationTableName = scoreItemRelationTableName;
        }

        public string ScoreItemRelationTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task ReplacePages(Guid ownerId, Guid scoreId, List<PatchScorePage> pages)
        {
            if (pages.Count == 0)
            {
                throw new ArgumentException(nameof(pages));
            }

            var trimmedPages = new List<PatchScorePage>();

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var trimmedPage = page.Page?.Trim();

                if (trimmedPage is null)
                {
                    throw new ArgumentException($"{nameof(pages)}[{i}].Page is null.");
                }

                var trimmedObjectName = page.ObjectName?.Trim();
                if (string.IsNullOrWhiteSpace(trimmedObjectName))
                {
                    throw new ArgumentException($"{nameof(pages)}[{i}].ObjectName is empty.");
                }

                trimmedPages.Add(new PatchScorePage()
                {
                    TargetPageId = page.TargetPageId,
                    Page = trimmedPage,
                    ItemId = page.ItemId,
                    ObjectName = trimmedObjectName,
                });
            }

            await ReplacePagesAsync(ownerId, scoreId, trimmedPages);
        }


        public async Task ReplacePagesAsync(Guid ownerId, Guid scoreId, List<PatchScorePage> pages)
        {

            if (pages.Count == 0)
                throw new ArgumentException(nameof(pages));

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            data.Page ??= new List<DynamoDbScorePageV1>();

            var removeRelationItemSet = new HashSet<string>();
            var newRelationItemSet = new HashSet<string>();

            foreach (var pageV1 in data.Page)
            {
                removeRelationItemSet.Add(pageV1.ItemId);
            }

            // Key id, Value index
            var pageIndices = new Dictionary<long,int>();
            foreach (var (databaseScoreDataPageV1,index) in data.Page.Select((x,index)=>(x,index)))
            {
                pageIndices[databaseScoreDataPageV1.Id] = index;
            }

            var replacingPages = new List<(DynamoDbScorePageV1 data, int targetIndex)>();

            foreach (var page in pages)
            {
                var id = page.TargetPageId;
                if(!pageIndices.TryGetValue(id, out var index))
                    throw new InvalidOperationException();

                var itemId = ScoreDatabaseUtils.ConvertToBase64(page.ItemId);
                var p = new DynamoDbScorePageV1()
                {
                    Id = id,
                    ItemId = itemId,
                    Page = page.Page,
                    ObjectName = page.ObjectName,
                };
                replacingPages.Add((p, index));
                data.Page[index] = p;
                newRelationItemSet.Add(itemId);
            }

            foreach (var pageV1 in data.Page)
            {
                removeRelationItemSet.Remove(pageV1.ItemId);
            }

            var newHash = data.CalcDataHash();

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId, replacingPages, newHash, oldHash, now);

            await PutItemRelations(_dynamoDbClient, ScoreItemRelationTableName, ownerId, scoreId, newRelationItemSet);

            await DeleteItemRelations(_dynamoDbClient, ScoreItemRelationTableName, ownerId, scoreId, removeRelationItemSet);


            static async Task<(DynamoDbScoreDataV1 data, string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid scoreId)
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result, hash);
            }


            static AttributeValue ConvertFromPage(DynamoDbScorePageV1 page)
            {
                var p = new Dictionary<string, AttributeValue>()
                {
                    [DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Id] = new AttributeValue()
                    {
                        N = page.Id.ToString(),
                    }
                };
                if (page.Page != null)
                {
                    p[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Page] = new AttributeValue(page.Page);
                }
                if (page.ItemId != null)
                {
                    p[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.ItemId] = new AttributeValue(page.ItemId);
                }
                if (page.ObjectName != null)
                {
                    p[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.ObjectName] = new AttributeValue(page.ObjectName);
                }
                if(p.Count == 0)
                    return null;

                return new AttributeValue() {M = p};
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid scoreId,
                List<(DynamoDbScorePageV1 data, int targetIndex)> replacingPages,
                string newHash,
                string oldHash,
                DateTimeOffset now
            )
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var replacingValues = replacingPages
                    .Select(x => (key: ":newPage" + x.targetIndex, value: ConvertFromPage(x.data), x.targetIndex))
                    .ToArray();
                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#pages"] = DynamoDbScorePropertyNames.DataPropertyNames.Pages,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>(replacingValues.ToDictionary(x=>x.key, x=>x.value))
                    {
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression =
                        $"SET #updateAt = :updateAt, #hash = :newHash, {string.Join(", ", replacingValues.Select((x)=>$"#data.#pages[{x.targetIndex}] = {x.key}"))}",
                    TableName = tableName,
                };
                try
                {
                    await client.UpdateItemAsync(request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

            }


            static async Task PutItemRelations(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, HashSet<string> items)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var chunkList = items.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await PutItemRelations25Async(client, tableName, partitionKey, score, ids);
                }

                static async Task PutItemRelations25Async(IAmazonDynamoDB client, string tableName, string partitionKey, string score, string[] ids)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = ids.Select(id=>new WriteRequest()
                        {
                            PutRequest = new PutRequest()
                            {
                                Item = new Dictionary<string, AttributeValue>()
                                {
                                    [DynamoDbScoreItemRelationPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                    [DynamoDbScoreItemRelationPropertyNames.ItemRelation] = new AttributeValue(id + score),
                                }
                            }

                        }).ToList(),
                    };

                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 削除時に失敗したデータを取得しリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            }


            static async Task DeleteItemRelations(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, HashSet<string> items)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var chunkList = items.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await DeleteItemRelations25Async(client, tableName, partitionKey, score, ids);
                }

                static async Task DeleteItemRelations25Async(IAmazonDynamoDB client, string tableName, string partitionKey, string score, string[] ids)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = ids.Select(id=>new WriteRequest()
                        {
                            DeleteRequest = new DeleteRequest()
                            {
                                Key = new Dictionary<string, AttributeValue>()
                                {
                                    [DynamoDbScoreItemRelationPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                    [DynamoDbScoreItemRelationPropertyNames.ItemRelation] = new AttributeValue(id + score),
                                }
                            }

                        }).ToList(),
                    };

                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 削除時に失敗したデータを取得しリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            }
        }
    }
}
