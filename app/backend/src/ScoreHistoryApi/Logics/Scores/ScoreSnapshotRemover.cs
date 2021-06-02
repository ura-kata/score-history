using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSnapshotRemover
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public string ScoreTableName { get; }
        public string ScoreItemRelationTableName { get; }
        public string ScoreSnapshotBucketName { get; }

        public ScoreSnapshotRemover(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _configuration = configuration;

            var bucketName = configuration[EnvironmentNames.ScoreDataSnapshotS3Bucket];
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDataSnapshotS3Bucket}' is not found.");
            }
            ScoreSnapshotBucketName = bucketName;

            var scoreTableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreTableName))
            {
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            }
            ScoreTableName = scoreTableName;

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
            {
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");
            }
            ScoreItemRelationTableName = scoreItemRelationTableName;
        }

        public async Task RemoveAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            var detail = await GetAsync(ownerId, scoreId, snapshotId);

            var itemIds = detail.Data.Pages.Select(x => x.ItemId).ToArray();

            await DeleteSnapshotAsync(ownerId, scoreId, snapshotId);

            await DeleteItemRelationsAsync(ownerId, snapshotId, itemIds);

            await DeleteSnapshotDataFromStorageAsync(ownerId, scoreId, snapshotId);
        }

        public async Task<ScoreSnapshotDetail> GetAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            var key = ScoreSnapshotStorageUtils.CreateSnapshotKey(ownerId, scoreId, snapshotId);

            var request = new GetObjectRequest()
            {
                BucketName = ScoreSnapshotBucketName,
                Key = key,
            };
            try
            {
                var response = await _s3Client.GetObjectAsync(request);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    byte[] buffer = new byte[response.ResponseStream.Length];
                    await response.ResponseStream.ReadAsync(buffer);

                    return ScoreSnapshotStorageUtils.MapFromJson(buffer);
                }
            }
            catch (Exception ex)
            {
                throw new NotFoundSnapshotException(ex);
            }
            throw new NotFoundSnapshotException("Not found snapshot.");
        }

        public async Task DeleteItemRelationsAsync(Guid ownerId, Guid snapshotId, Guid[] itemIds)
        {
            const int chunkSize = 25;

            var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);
            var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

            var chunkList = itemIds
                .Select((x, index) => (x:ScoreDatabaseUtils.ConvertToBase64(x) + snapshot, index))
                .GroupBy(x => x.index / chunkSize)
                .Select(x=>x.Select(y=> y.x).ToArray())
                .ToArray();

            foreach (var ids in chunkList)
            {
                await DeleteData25Async(_dynamoDbClient, ScoreItemRelationTableName, partitionKey, ids);
            }

            static async Task DeleteData25Async(IAmazonDynamoDB client, string tableName, string partitionKey, string[] ids)
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
                                [DynamoDbScoreItemRelationPropertyNames.ItemRelation] = new AttributeValue(id),
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

        public async Task DeleteSnapshotDataFromStorageAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            var key = ScoreSnapshotStorageUtils.CreateSnapshotKey(ownerId, scoreId, snapshotId);

            var request2 = new DeleteObjectRequest()
            {
                BucketName = ScoreSnapshotBucketName,
                Key = key
            };
            await _s3Client.DeleteObjectAsync(request2);
        }

        public async Task DeleteSnapshotAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
            var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

            await DeleteItemAsync(_dynamoDbClient, ScoreTableName, partitionKey, score, snapshot);

            static async Task DeleteItemAsync(
                IAmazonDynamoDB client,
                string tableName,
                string partitionKey,
                string score,
                string snapshot
                )
            {
                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshot),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                            },
                            ConditionExpression = "attribute_exists(#score)",
                        },
                    },
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#snapshotCount"] = DynamoDbScorePropertyNames.SnapshotCount,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "-1"},
                            },
                            UpdateExpression = "ADD #snapshotCount :increment",
                        }
                    },
                };

                try
                {
                    await client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                    {
                        TransactItems = actions,
                        ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL,
                    });
                }
                catch (TransactionCanceledException ex)
                {
                    var deleteReason = ex.CancellationReasons[0];

                    if (deleteReason.Code == "ConditionalCheckFailed")
                    {
                        throw new NotFoundSnapshotException(ex);
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }
    }
}
