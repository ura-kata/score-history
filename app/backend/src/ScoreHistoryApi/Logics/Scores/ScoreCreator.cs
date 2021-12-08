#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.DynamoDb;
using ScoreHistoryApi.Logics.DynamoDb.PropertyNames;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreCreator
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;
        private readonly IScoreCommonLogic _commonLogic;

        public ScoreCreator(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration, IScoreCommonLogic commonLogic)
        {
            _dynamoDbClient = dynamoDbClient;
            _quota = quota;
            _configuration = configuration;
            _commonLogic = commonLogic;

            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

        }

        public string ScoreTableName { get; set; }


        /// <summary>
        /// 楽譜の作成
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="newScore"></param>
        /// <returns></returns>
        public async Task<NewlyScore> CreateAsync(Guid ownerId, NewScore newScore)
        {
            var title = newScore.Title;
            var description = newScore.Description;

            if (title == null)
                throw new ArgumentNullException(nameof(newScore));

            var preprocessingTitle = title.Trim();
            if (preprocessingTitle == "")
                throw new ArgumentException(nameof(newScore));

            if (_quota.TitleLengthMax < preprocessingTitle.Length)
                throw new ArgumentException(nameof(newScore));


            var preprocessingDescription = description?.Trim();

            if (_quota.DescriptionLengthMax < preprocessingDescription?.Length)
                throw new ArgumentException(nameof(newScore));


            var newScoreId = _commonLogic.NewGuid();

            return await CreateAsync(ownerId, newScoreId, preprocessingTitle, preprocessingDescription);
        }

        public async Task<NewlyScore> CreateAsync(Guid ownerId,Guid newScoreId, string title, string? description)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var scoreCountMax = _quota.ScoreCountMax;

            var now = _commonLogic.Now;


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

            var scorePartitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);
            var newScore = _commonLogic.ConvertIdFromGuid(newScoreId);

            var createAt = now.ToUnixTimeMilliseconds();
            var updateAt = createAt;

            var newLockSummary = _commonLogic.NewGuid();
            var newLockSummaryValue = _commonLogic.ConvertIdFromGuid(newLockSummary);

            var newLockMainValue = _commonLogic.NewLock();

            var actions = new List<TransactWriteItem>()
            {
                new()
                {
                    Update = new Update()
                    {
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreSummaryPn.PartitionKey] = new(scorePartitionKey),
                            [ScoreSummaryPn.SortKey] = new(DynamoDbConstant.SummarySortKey),
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#sc"] = ScoreSummaryPn.ScoreCount,
                            ["#lock"] = ScoreSummaryPn.Lock,
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                        {
                            [":inc"] = new (){N="1"},
                            [":newL"] = new (newLockSummaryValue),
                            [":countMax"] = new()
                            {
                                N = maxCount.ToString()
                            },
                        },
                        ConditionExpression = "#sc < :countMax",
                        UpdateExpression = "SET #lock = :newL ADD #sc :inc",
                        TableName = tableName,
                    },
                },
                new()
                {
                    Put = new Put()
                    {
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreMainPn.PartitionKey] = new(scorePartitionKey),
                            [ScoreMainPn.SortKey] = new(newScore),
                            [ScoreMainPn.CreateAt] = new(){N = createAt.ToString()},
                            [ScoreMainPn.UpdateAt] = new(){N = updateAt.ToString()},
                            [ScoreMainPn.Access] = new(ScoreAccessKind.Private),
                            [ScoreMainPn.Lock] = new(newLockMainValue),
                            [ScoreMainPn.TransactionTimeout] = new() {N = "0"},
                            [ScoreMainPn.TransactionStart] = new() {N = "0"},
                            [ScoreMainPn.Ver] = new(){N = DynamoDbConstant.Ver1},
                            [ScoreMainPn.SnapshotCount] = new() {N = "0"},
                            [ScoreMainPn.Snapshot] = new() {IsLSet = true},
                            [ScoreMainPn.Data] = new ()
                            {
                                M = new Dictionary<string, AttributeValue>()
                                {
                                    [ScoreMainPn.DataPn.Title] = new(title),
                                    [ScoreMainPn.DataPn.Description] = new(description),
                                    [ScoreMainPn.DataPn.PageCount] = new(){N = "0"},
                                    [ScoreMainPn.DataPn.Page] = new(){IsLSet = true},
                                    [ScoreMainPn.DataPn.AnnotationCount] = new(){N = "0"},
                                    [ScoreMainPn.DataPn.Annotation] = new(){IsLSet = true},
                                }
                            },
                        },
                        TableName = tableName,
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#s"] = ScoreMainPn.SortKey,
                        },
                        ConditionExpression = "attribute_not_exists(#s)",
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
                            [ScoreSummaryPn.PartitionKey] = new(scorePartitionKey),
                            [ScoreSummaryPn.SortKey] = new(DynamoDbConstant.SummarySortKey),
                        },
                    };
                    var checkResponse = await client.GetItemAsync(request);

                    if (checkResponse.Item.TryGetValue(ScoreSummaryPn.ScoreCount, out _))
                    {
                        throw new CreatedScoreException(CreatedScoreExceptionCodes.ExceededUpperLimit, ex);
                    }
                    else
                    {
                        throw new UninitializedScoreException(ex);
                    }
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
