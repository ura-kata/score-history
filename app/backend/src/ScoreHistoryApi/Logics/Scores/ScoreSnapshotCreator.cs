using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSnapshotCreator
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreSnapshotCreator(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IScoreQuota quota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _quota = quota;
            _configuration = configuration;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreDataTableName = configuration[EnvironmentNames.ScoreLargeDataDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");
            ScoreDataTableName = scoreDataTableName;

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");
            ScoreItemRelationTableName = scoreItemRelationTableName;


            var scoreDataSnapshotS3Bucket = configuration[EnvironmentNames.ScoreDataSnapshotS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreDataSnapshotS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreDataSnapshotS3Bucket}' is not found.");
            ScoreDataSnapshotS3Bucket = scoreDataSnapshotS3Bucket;
        }

        public string ScoreDataSnapshotS3Bucket { get; set; }

        public string ScoreItemRelationTableName { get; set; }

        public string ScoreDataTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task CreateAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {
            if (snapshotName is null)
            {
                throw new ArgumentNullException(nameof(snapshotName));
            }

            var trimSnapshotName = snapshotName.Trim();

            if (string.IsNullOrWhiteSpace(trimSnapshotName))
            {
                throw new ArgumentException(nameof(snapshotName));
            }

            // TODO ハッシュを確認して同じスナップショットを作成しないようにする

            var snapshot = await CreateSnapshotAsync(ownerId, scoreId, trimSnapshotName);

            var access = snapshot.access == ScoreAccesses.Public
                ? ScoreObjectAccessControls.Public
                : ScoreObjectAccessControls.Private;
            await CreateSnapshotItemAsync(ownerId, scoreId, snapshot.snapshot, access);
        }


        public async Task CreateSnapshotItemAsync(Guid ownerId, Guid scoreId, ScoreSnapshotDetail snapshotDetail,
            ScoreObjectAccessControls accessControl)
        {
            var key = ScoreSnapshotStorageUtils.CreateSnapshotKey(ownerId, scoreId, snapshotDetail.Id);

            var json = ScoreSnapshotStorageUtils.ConvertToJson(snapshotDetail);

            await using var jsonStream = new MemoryStream(json);

            var acl = accessControl switch
            {
                ScoreObjectAccessControls.Private => S3CannedACL.Private,
                ScoreObjectAccessControls.Public => S3CannedACL.PublicRead,
                _ => throw new NotSupportedException(),
            };

            var request = new PutObjectRequest()
            {
                BucketName = ScoreDataSnapshotS3Bucket,
                Key = key,
                CannedACL = acl,
                InputStream = jsonStream,
            };
            await _s3Client.PutObjectAsync(request);
        }


        public async Task<(ScoreSnapshotDetail snapshot, ScoreAccesses access)> CreateSnapshotAsync(Guid ownerId,
            Guid scoreId,
            string snapshotName)
        {
            // TODO ここで作成されたデータを使い JSON ファイルを作成し S3 に保存する

            var snapshotId = Guid.NewGuid();
            var response = await CreateSnapshotAsync(ownerId, scoreId, snapshotId, snapshotName);
            var snapshot = ScoreSnapshotDetail.Create(snapshotId, snapshotName, response.dynamoDbScore, response.hashSet);

            var access = ScoreDatabaseUtils.ConvertToScoreAccess(response.dynamoDbScore.Access);
            return (snapshot, access);
        }

        public async Task<(DynamoDbScore dynamoDbScore, Dictionary<string, string> hashSet)>
            CreateSnapshotAsync(Guid ownerId, Guid scoreId, Guid snapshotId, string snapshotName)
        {
            var dynamoDbScore = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            var hashSet = await GetAnnotationsAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId);

            var descriptionHash = dynamoDbScore.Data.GetDescriptionHash();
            var (success, description) =
                await TryGetDescriptionAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId, descriptionHash);

            var itemRelationIds = dynamoDbScore.Data.GetPages().Select(x => x.ItemId).ToArray();

            if (success)
            {
                hashSet[descriptionHash] = description;
            }

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            var maxSnapshotCount = _quota.SnapshotCountMax;
            await UpdateAsync(
                _dynamoDbClient, ScoreTableName, ownerId, scoreId, snapshotId
                , snapshotName, now, maxSnapshotCount);

            await PutItemRelations(_dynamoDbClient, ScoreItemRelationTableName, ownerId, snapshotId, itemRelationIds);


            return (dynamoDbScore, hashSet);

            static async Task<DynamoDbScore> GetAsync(
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
                        [DynamoDbScorePropertyNames.SortKey] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);

                var dynamoDbScore = new DynamoDbScore(response.Item);

                return dynamoDbScore;
            }

            static async Task<Dictionary<string, string>> GetAnnotationsAsync(
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
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
                        ["#content"] = DynamoDbScoreDataPropertyNames.Content,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                        [":annScore"] = new AttributeValue(DynamoDbScoreDataConstant.PrefixAnnotation + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#data, :annScore)",
                    ProjectionExpression = "#data, #content",
                };

                try
                {
                    var response = await client.QueryAsync(request);

                    var result = new Dictionary<string, string>();
                    var substringStartIndex = DynamoDbScoreDataConstant.PrefixAnnotation.Length + score.Length;
                    foreach (var item in response.Items)
                    {
                        var hashValue = item[DynamoDbScoreDataPropertyNames.DataId];
                        var hash = hashValue.S.Substring(substringStartIndex);
                        var contentValue = item[DynamoDbScoreDataPropertyNames.Content];
                        result[hash] = contentValue.S;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

            }

            static async Task<(bool success,string description)> TryGetDescriptionAsync(
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, string descriptionHash)
            {
                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                        [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(DynamoDbScoreDataConstant.PrefixDescription + score + descriptionHash),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#content"] = DynamoDbScoreDataPropertyNames.Content,

                    },
                    ProjectionExpression = "#content",
                };

                try
                {
                    var response = await client.GetItemAsync(request);

                    if (!response.IsItemSet)
                    {
                        return (false, "");
                    }

                    var description = response.Item[DynamoDbScoreDataPropertyNames.Content].S;
                    return (true, description);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid scoreId,
                Guid snapshotId,
                string snapshotName,
                DateTimeOffset now,
                int maxSnapshotCount
                )
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
                var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

                var at = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.SortKey] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#snapshotCount"] = DynamoDbScorePropertyNames.SnapshotCount,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "1"},
                                [":countMax"] = new AttributeValue()
                                {
                                    N = maxSnapshotCount.ToString(),
                                },
                            },
                            ConditionExpression = "#snapshotCount < :countMax",
                            UpdateExpression = "ADD #snapshotCount :increment"
                        }
                    },
                    new TransactWriteItem()
                    {
                        Put = new Put()
                        {
                            TableName = tableName,
                            Item = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.SortKey] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshot),
                                [DynamoDbScorePropertyNames.CreateAt] = new AttributeValue(at),
                                [DynamoDbScorePropertyNames.UpdateAt] = new AttributeValue(at),
                                [DynamoDbScorePropertyNames.SnapshotName] = new AttributeValue(snapshotName),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.SortKey,
                            },
                            ConditionExpression = "attribute_not_exists(#score)",
                        }
                    }
                };

                try
                {
                    await client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                    {
                        TransactItems = actions,
                        ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
                    });
                }
                catch (TransactionCanceledException ex)
                {
                    var updateReason = ex.CancellationReasons[0];

                    if (updateReason.Code == "ConditionalCheckFailed")
                    {
                        throw new CreatedSnapshotException(CreatedSnapshotExceptionCodes.ExceededUpperLimit, ex);
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }


            static async Task PutItemRelations(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid snapshotId, string[] items)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);
                var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

                var chunkList = items.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await PutItemRelations25Async(client, tableName, partitionKey, snapshot, ids);
                }

                static async Task PutItemRelations25Async(IAmazonDynamoDB client, string tableName, string partitionKey, string snapshot, string[] ids)
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
                                    [DynamoDbScoreItemRelationPropertyNames.ItemRelation] = new AttributeValue(id + snapshot),
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
