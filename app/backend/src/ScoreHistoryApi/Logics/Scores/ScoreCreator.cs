#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreCreator
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreCreator(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration)
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


        /// <summary>
        /// 楽譜の作成
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="newScore"></param>
        /// <returns></returns>
        public async Task<NewlyScore> CreateAsync(Guid ownerId, NewScore newScore)
        {
            return await CreateAsync(ownerId, newScore.Title, newScore.Description);
        }

        public async Task<NewlyScore> CreateAsync(Guid ownerId, string title, string? description)
        {
            var newScoreId = Guid.NewGuid();
            return await CreateAsync(ownerId, newScoreId, title, description);
        }

        public async Task<NewlyScore> CreateAsync(Guid ownerId, Guid newScoreId, string title, string? description)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var preprocessingTitle = title.Trim();
            if (preprocessingTitle == "")
                throw new ArgumentException(nameof(title));

            var scoreCountMax = _quota.ScoreCountMax;

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await PutScoreAsync(
                 ownerId, newScoreId, scoreCountMax,
                title, description ?? "", now);

            return new NewlyScore()
            {
                Id = newScoreId
            };
        }

        async Task PutScoreAsync(
            Guid ownerId,
            Guid newScoreId,
            int maxCount,
            string title,
            string description,
            DateTimeOffset now)
        {
            var client = _dynamoDbClient;
            var tableName = ScoreTableName;
            var dataTableName = ScoreDataTableName;

            var scorePartitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
            var largeDataPartitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
            var newScore = ScoreDatabaseUtils.ConvertToBase64(newScoreId);

            var descriptionHash =
                DynamoDbScoreDataUtils.CalcHash(DynamoDbScoreDataUtils.DescriptionPrefix, description ?? "");
            var data = new DynamoDbScoreDataV1()
            {
                Title = title,
                DescriptionHash = descriptionHash,
            };
            var dataAttributeValue = data.ConvertToAttributeValue();
            var dataHash = data.CalcDataHash();
            var createAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);
            var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

            var dataId = DynamoDbScoreDataConstant.PrefixDescription + newScore + descriptionHash;
            var actions = new List<TransactWriteItem>()
            {
                new TransactWriteItem()
                {
                    Update = new Update()
                    {
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(scorePartitionKey),
                            [DynamoDbScorePropertyNames.SortKey] =
                                new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#count"] = DynamoDbScorePropertyNames.ScoreCount,
                            ["#scores"] = DynamoDbScorePropertyNames.Scores,
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                        {
                            [":increment"] = new AttributeValue() {N = "1"},
                            [":countMax"] = new AttributeValue()
                            {
                                N = maxCount.ToString()
                            },
                            [":newScore"] = new AttributeValue()
                            {
                                SS = new List<string>() {newScore}
                            }
                        },
                        ConditionExpression = "#count < :countMax",
                        UpdateExpression = "ADD #count :increment, #scores :newScore",
                        TableName = tableName,
                    },
                },
                new TransactWriteItem()
                {
                    Put = new Put()
                    {
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(scorePartitionKey),
                            [DynamoDbScorePropertyNames.SortKey] =
                                new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + newScore),
                            [DynamoDbScorePropertyNames.DataHash] = new AttributeValue(dataHash),
                            [DynamoDbScorePropertyNames.CreateAt] = new AttributeValue(createAt),
                            [DynamoDbScorePropertyNames.UpdateAt] = new AttributeValue(updateAt),
                            [DynamoDbScorePropertyNames.Access] =
                                new AttributeValue(ScoreDatabaseConstant.ScoreAccessPrivate),
                            [DynamoDbScorePropertyNames.SnapshotCount] = new AttributeValue() {N = "0"},
                            [DynamoDbScorePropertyNames.Data] = dataAttributeValue,
                        },
                        TableName = tableName,
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#score"] = DynamoDbScorePropertyNames.SortKey,
                        },
                        ConditionExpression = "attribute_not_exists(#score)",
                    }
                },
                new TransactWriteItem()
                {
                    Put = new Put()
                    {
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(largeDataPartitionKey),
                            [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(dataId),
                            [DynamoDbScoreDataPropertyNames.Content] = new AttributeValue(description),
                        },
                        TableName = dataTableName,
                    }
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
                var updateReason = ex.CancellationReasons[0];

                if (updateReason.Code == "ConditionalCheckFailed")
                {
                    var request = new GetItemRequest()
                    {
                        TableName = tableName,
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(scorePartitionKey),
                            [DynamoDbScorePropertyNames.SortKey] =
                                new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                        },
                    };
                    var checkResponse = await client.GetItemAsync(request);

                    if (checkResponse.Item.TryGetValue(DynamoDbScorePropertyNames.ScoreCount, out _))
                    {
                        throw new CreatedScoreException(CreatedScoreExceptionCodes.ExceededUpperLimit, ex);
                    }
                    else
                    {
                        throw new UninitializedScoreException(ex);
                    }

                    ;
                }

                var putReason = ex.CancellationReasons[1];

                if (putReason.Code == "ConditionalCheckFailed")
                {
                    throw new ExistedScoreException(ex);
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
