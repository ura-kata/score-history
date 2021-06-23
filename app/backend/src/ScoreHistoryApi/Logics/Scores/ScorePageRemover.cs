using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScorePageRemover
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;

        public ScorePageRemover(IAmazonDynamoDB dynamoDbClient, IScoreQuota scoreQuota, IConfiguration configuration)
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

        public async Task RemovePages(Guid ownerId, Guid scoreId, List<long> pageIds)
        {
            if (pageIds.Count == 0)
            {
                throw new ArgumentException(nameof(pageIds));
            }

            await RemovePagesAsync(ownerId, scoreId, pageIds);
        }


        public async Task RemovePagesAsync(Guid ownerId, Guid scoreId, List<long> pageIds)
        {

            if (pageIds.Count == 0)
                throw new ArgumentException(nameof(pageIds));

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            data.Page ??= new List<DynamoDbScorePageV1>();

            var removeItemRelationSet = new HashSet<string>();

            foreach (var pageV1 in data.Page)
            {
                removeItemRelationSet.Add(pageV1.ItemId);
            }

            var existedIdSet = new HashSet<long>();
            pageIds.ForEach(id => existedIdSet.Add(id));

            var removeIndices = data.Page.Select((x, index) => (x, index))
                .Where(x => x.x != null && existedIdSet.Contains(x.x.Id))
                .Select(x => x.index)
                .ToArray();

            foreach (var index in removeIndices.Reverse())
            {
                data.Page.RemoveAt(index);
            }

            foreach (var pageV1 in data.Page)
            {
                removeItemRelationSet.Remove(pageV1.ItemId);
            }

            var newHash = data.CalcDataHash();

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId, removeIndices, newHash, oldHash, now);

            await DeleteItemRelations(_dynamoDbClient, ScoreItemRelationTableName, ownerId, scoreId, removeItemRelationSet);

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

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid scoreId,
                int[] removeIndices,
                string newHash,
                string oldHash,
                DateTimeOffset now
            )
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

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
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression =
                        $"SET #updateAt = :updateAt, #hash = :newHash REMOVE {string.Join(", ", removeIndices.Select(i=>$"#data.#pages[{i}]"))}",
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


            static async Task DeleteItemRelations(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, HashSet<string> items)
            {
                var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                const int chunkSize = 25;

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
