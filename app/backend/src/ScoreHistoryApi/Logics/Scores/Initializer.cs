using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.DynamoDb;
using ScoreHistoryApi.Logics.DynamoDb.PropertyNames;
using ScoreHistoryApi.Logics.Exceptions;

namespace ScoreHistoryApi.Logics.Scores
{
    public class Initializer
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;
        private readonly IScoreCommonLogic _commonLogic;

        public Initializer(IAmazonDynamoDB dynamoDbClient, IScoreQuota scoreQuota, IConfiguration configuration, IScoreCommonLogic commonLogic)
        {
            _dynamoDbClient = dynamoDbClient;
            _scoreQuota = scoreQuota;
            _configuration = configuration;
            _commonLogic = commonLogic;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreItemTableName = configuration[EnvironmentNames.ScoreItemDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemDynamoDbTableName}' is not found.");
            ScoreItemTableName = scoreItemTableName;
        }

        public string ScoreItemTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task Initialize(Guid ownerId)
        {
            try
            {
                await InitializeScoreAsync(ownerId);
            }
            catch (AlreadyInitializedException)
            {

                // 初期化済み
            }

            try
            {
                await InitializeScoreItemAsync(ownerId);
            }
            catch (AlreadyInitializedException)
            {

                // 初期化済み
            }
        }


        public async Task InitializeScoreAsync(Guid ownerId)
        {
            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);

            var newLockValue = _commonLogic.NewGuid();
            var newLock = _commonLogic.ConvertIdFromGuid(newLockValue);

            await PutAsync(_dynamoDbClient, ScoreTableName, partitionKey, newLock);

            static async Task PutAsync(IAmazonDynamoDB client, string tableName, string partitionKey, string newLock)
            {
                var request = new PutItemRequest()
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreSummaryPn.PartitionKey] = new(partitionKey),
                        [ScoreSummaryPn.SortKey] = new(DynamoDbConstant.SummarySortKey),
                        [ScoreSummaryPn.ScoreCount] = new(){N = "0"},
                        [ScoreSummaryPn.Lock] = new(){S = newLock}
                    },
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#o"] = ScoreSummaryPn.PartitionKey
                    },
                    ConditionExpression = "attribute_not_exists(#o)",
                };
                try
                {
                    await client.PutItemAsync(request);
                }
                catch (ConditionalCheckFailedException ex)
                {
                    if (ex.ErrorCode == "ConditionalCheckFailedException")
                    {
                        throw new AlreadyInitializedException(ex);
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


        public async Task InitializeScoreItemAsync(Guid ownerId)
        {
            var partitionKey = PartitionPrefix.Item + _commonLogic.ConvertIdFromGuid(ownerId);
            var newLockValue = _commonLogic.NewGuid();
            var newLock = _commonLogic.ConvertIdFromGuid(newLockValue);

            await PutAsync(_dynamoDbClient, ScoreItemTableName, partitionKey, newLock);

            static async Task PutAsync(IAmazonDynamoDB client, string tableName, string partitionKey, string newLock)
            {
                var request = new PutItemRequest()
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [ItemSummaryPn.PartitionKey] = new(partitionKey),
                        [ItemSummaryPn.SortKey] = new(DynamoDbConstant.SummarySortKey),
                        [ItemSummaryPn.TotalSize] = new(){N = "0"},
                        [ItemSummaryPn.TotalCount] = new(){N = "0"},
                        [ItemSummaryPn.Lock] = new(){S = newLock},
                    },
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#o"] = ItemSummaryPn.PartitionKey
                    },
                    ConditionExpression = "attribute_not_exists(#o)",
                };
                try
                {
                    await client.PutItemAsync(request);
                }
                catch (ConditionalCheckFailedException ex)
                {
                    if (ex.ErrorCode == "ConditionalCheckFailedException")
                    {
                        throw new AlreadyInitializedException(ex);
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
