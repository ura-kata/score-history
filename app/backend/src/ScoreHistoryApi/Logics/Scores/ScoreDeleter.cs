using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreDeleter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;

        public ScoreDeleter(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IScoreQuota scoreQuota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _scoreQuota = scoreQuota;
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

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");
            ScoreItemRelationTableName = scoreItemRelationTableName;



            var scoreItemS3Bucket = configuration[EnvironmentNames.ScoreItemS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreItemS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemS3Bucket}' is not found.");
            ScoreItemS3Bucket = scoreItemS3Bucket;

            var scoreDataSnapshotS3Bucket = configuration[EnvironmentNames.ScoreDataSnapshotS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreDataSnapshotS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreDataSnapshotS3Bucket}' is not found.");
            ScoreDataSnapshotS3Bucket = scoreDataSnapshotS3Bucket;
        }

        public string ScoreItemRelationTableName { get; set; }

        public string ScoreDataSnapshotS3Bucket { get; set; }

        public string ScoreItemS3Bucket { get; set; }

        public string ScoreDataTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task DeleteAsync(Guid ownerId, Guid scoreId)
        {
            // ここでは DynamoDB の楽譜の構造のみを削除する
            // Item の削除は別の API で削除を行う

            await DeleteScoreAsync(ownerId, scoreId);
            await DeleteAllSnapshotAsync(ownerId, scoreId);
        }


        public async Task DeleteAllSnapshotAsync(Guid ownerId, Guid scoreId)
        {
            var objectKeyList = new List<string>();
            string continuationToken = default;

            var prefix = $"{ownerId:D}/{scoreId:D}/{ScoreSnapshotStorageConstant.SnapshotFolderName}";

            do
            {
                var listRequest = new ListObjectsV2Request()
                {
                    BucketName = ScoreDataSnapshotS3Bucket,
                    Prefix = prefix,
                    ContinuationToken = string.IsNullOrWhiteSpace(continuationToken) ? null : continuationToken,
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                objectKeyList.AddRange(listResponse.S3Objects.Select(x => x.Key));

                continuationToken = listResponse.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            var request = new DeleteObjectsRequest()
            {
                BucketName = ScoreDataSnapshotS3Bucket,
                Objects = objectKeyList.Select(x=>new KeyVersion()
                {
                    Key = x
                }).ToList(),
            };
            await _s3Client.DeleteObjectsAsync(request);
        }

        public async Task DeleteScoreAsync(Guid ownerId, Guid scoreId)
        {
            await DeleteMainAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            // スナップショットの削除は SQS を使ったほうがいいかも
            var snapshotScoreIds = await GetSnapshotScoreIdsAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            await DeleteSnapshotsAsync(_dynamoDbClient, ScoreTableName, ownerId, snapshotScoreIds);

            var dataIds = await GetScoreAnnotationDataIdsAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId);
            var descriptionDataIds =
                await GetScoreDescriptionDataIdsAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId);

            await DeleteDataAsync(_dynamoDbClient, ScoreDataTableName, ownerId, dataIds.Concat(descriptionDataIds).ToArray());

            var itemRelations = await GetItemRelations(_dynamoDbClient, ScoreItemRelationTableName, ownerId);

            await DeleteItemRelations(_dynamoDbClient, ScoreItemRelationTableName, ownerId, itemRelations);

            static async Task DeleteMainAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                            },
                            ConditionExpression = "attribute_exists(#score)",
                            TableName = tableName,
                        }
                    },
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#count"] = DynamoDbScorePropertyNames.ScoreCount,
                                ["#scores"] = DynamoDbScorePropertyNames.Scores,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "-1"},
                                [":score"] = new AttributeValue()
                                {
                                    SS = new List<string>(){score}
                                }
                            },
                            UpdateExpression = "ADD #count :increment DELETE #scores :score",
                            TableName = tableName,
                        },
                    },
                };
                try
                {
                    await client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                    {
                        TransactItems = actions,
                        ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
                    });
                }
                catch (ResourceNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (InternalServerErrorException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (TransactionCanceledException ex)
                {
                    var deleteReason = ex.CancellationReasons[0];

                    if (deleteReason.Code == "ConditionalCheckFailed")
                    {
                        throw new NotFoundScoreException(ex);
                    }
                    var updateReason = ex.CancellationReasons[1];

                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            static async Task<string[]> GetSnapshotScoreIdsAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId,
                        ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                        [":snapPrefix"] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#score, :snapPrefix)",
                    ProjectionExpression = "#score",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items.Select(x => x[DynamoDbScorePropertyNames.ScoreId]?.S)
                        .Where(x => !(x is null))
                        .ToArray();
                }
                catch (InternalServerErrorException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ProvisionedThroughputExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (RequestLimitExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ResourceNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }


            static async Task DeleteSnapshotsAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, string[] scoreIds)
            {
                const int chunkSize = 25;

                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);

                var chunkList = scoreIds.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await DeleteSnapshot25Async(client, tableName, partitionKey, ids);
                }

                static async Task DeleteSnapshot25Async(IAmazonDynamoDB client, string tableName, string partitionKey, string[] scoreIds)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = scoreIds.Select(scoreId=>new WriteRequest()
                        {
                            DeleteRequest = new DeleteRequest()
                            {
                                Key = new Dictionary<string, AttributeValue>()
                                {
                                    [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                    [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(scoreId),
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

            static async Task<string[]> GetScoreAnnotationDataIdsAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScoreDataPropertyNames.OwnerId,
                        ["#data"] = DynamoDbScoreDataPropertyNames.DataId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                        [":annScore"] = new AttributeValue(DynamoDbScoreDataConstant.PrefixAnnotation + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#data, :annScore)",
                    ProjectionExpression = "#data",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items.Select(x => x[DynamoDbScoreDataPropertyNames.DataId]?.S)
                        .Where(x => !(x is null))
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            static async Task<string[]> GetScoreDescriptionDataIdsAsync(IAmazonDynamoDB client, string tableName, Guid ownerId,Guid scoreId)
            {
                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScoreDataPropertyNames.OwnerId,
                        ["#data"] = DynamoDbScoreDataPropertyNames.DataId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                        [":desScore"] = new AttributeValue(DynamoDbScoreDataConstant.PrefixDescription + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#data, :desScore)",
                    ProjectionExpression = "#data",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items.Select(x => x[DynamoDbScoreDataPropertyNames.DataId]?.S)
                        .Where(x => !(x is null))
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            static async Task DeleteDataAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, string[] dataIds)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);

                var chunkList = dataIds.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await DeleteData25Async(client, tableName, partitionKey, ids);
                }

                static async Task DeleteData25Async(IAmazonDynamoDB client, string tableName, string partitionKey, string[] dataIds)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = dataIds.Select(dataId=>new WriteRequest()
                        {
                            DeleteRequest = new DeleteRequest()
                            {
                                Key = new Dictionary<string, AttributeValue>()
                                {
                                    [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                    [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(dataId),
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

            static async Task<string[]> GetItemRelations(IAmazonDynamoDB client, string tableName, Guid ownerId)
            {
                var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScoreItemRelationPropertyNames.OwnerId,
                        ["#itemRelation"] = DynamoDbScoreItemRelationPropertyNames.ItemRelation,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                    },
                    KeyConditionExpression = "#owner = :owner",
                    ProjectionExpression = "#itemRelation",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items.Select(x => x[DynamoDbScoreItemRelationPropertyNames.ItemRelation]?.S)
                        .Where(x => !(x is null))
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            static async Task DeleteItemRelations(IAmazonDynamoDB client, string tableName, Guid ownerId, string[] itemRelations)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);

                var chunkList = itemRelations.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await DeleteData25Async(client, tableName, partitionKey, ids);
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

        }

    }
}
