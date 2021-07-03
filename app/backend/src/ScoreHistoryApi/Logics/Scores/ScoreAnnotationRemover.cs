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
    public class ScoreAnnotationRemover
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreAnnotationRemover(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _quota = quota;
            _configuration = configuration;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreDataTableName = configuration[EnvironmentNames.ScoreLargeDataDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreLargeDataDynamoDbTableName}' is not found.");
            ScoreDataTableName = scoreDataTableName;
        }

        public string ScoreDataTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task RemoveAnnotations(Guid ownerId, Guid scoreId, List<long> annotationIds)
        {
            if (annotationIds.Count == 0)
            {
                throw new ArgumentException(nameof(annotationIds));
            }

            await RemoveAnnotationsAsync(ownerId, scoreId, annotationIds);
        }



        public async Task RemoveAnnotationsAsync(Guid ownerId, Guid scoreId, List<long> annotationIds)
        {

            if (annotationIds.Count == 0)
                throw new ArgumentException(nameof(annotationIds));

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            data.Annotations ??= new List<DynamoDbScoreAnnotationV1>();

            var existedIdSet = new HashSet<long>();
            annotationIds.ForEach(id => existedIdSet.Add(id));

            var removeIndices = data.Annotations.Select((x, index) => (x, index))
                .Where(x => x.x != null && existedIdSet.Contains(x.x.Id))
                .Select(x => x.index)
                .ToArray();

            var removeHashSet = new HashSet<string>();
            foreach (var index in removeIndices.Reverse())
            {
                removeHashSet.Add(data.Annotations[index].ContentHash);
                data.Annotations.RemoveAt(index);
            }

            foreach (var annotation in data.Annotations)
            {
                if (removeHashSet.Contains(annotation.ContentHash))
                {
                    removeHashSet.Remove(annotation.ContentHash);
                }
            }

            var newHash = data.CalcDataHash();

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId, removeIndices, newHash, oldHash, now);

            if (removeHashSet.Count != 0)
            {
                await RemoveDataAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId, removeHashSet);
            }

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
                        [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.SortKey] =
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
                        [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.SortKey] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#ann"] = DynamoDbScorePropertyNames.DataPropertyNames.Annotations,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression =
                        $"SET #updateAt = :updateAt, #hash = :newHash REMOVE {string.Join(", ", removeIndices.Select(i=>$"#data.#ann[{i}]"))}",
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

            static async Task RemoveDataAsync(
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId,
                HashSet<string> removeHashSet)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var chunkList = removeHashSet.Select((h, index) => (h, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x => x.Select(x => x.h).ToArray())
                    .ToArray();

                foreach (var hashList in chunkList)
                {
                    await RemoveData25Async(client, tableName, partitionKey, score, hashList);
                }

                static async Task RemoveData25Async(
                    IAmazonDynamoDB client, string tableName, string partitionKey, string score,
                    string[] hashList)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = hashList.Select(h=>
                        {
                            var dataId = DynamoDbScoreDataConstant.PrefixAnnotation + score + h;
                            return new WriteRequest()
                            {
                                DeleteRequest = new DeleteRequest()
                                {
                                    Key = new Dictionary<string, AttributeValue>()
                                    {
                                        [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                        [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(dataId),
                                    },
                                },
                            };
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
