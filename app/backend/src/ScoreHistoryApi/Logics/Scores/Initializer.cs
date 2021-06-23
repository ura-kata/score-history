using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreItemDatabases;

namespace ScoreHistoryApi.Logics.Scores
{
    public class Initializer
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;

        public Initializer(IAmazonDynamoDB dynamoDbClient, IScoreQuota scoreQuota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _scoreQuota = scoreQuota;
            _configuration = configuration;


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
            catch (AlreadyInitializedException ex)
            {

                // 初期化済み
            }

            try
            {
                await InitializeScoreItemAsync(ownerId);
            }
            catch (AlreadyInitializedException ex)
            {

                // 初期化済み
            }
        }


        public async Task InitializeScoreAsync(Guid ownerId)
        {
            var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);

            await PutAsync(_dynamoDbClient, ScoreTableName, partitionKey);

            static async Task PutAsync(IAmazonDynamoDB client, string tableName, string partitionKey)
            {
                var request = new PutItemRequest()
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                        [DynamoDbScorePropertyNames.ScoreCount] = new AttributeValue(){N = "0"}
                    },
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId
                    },
                    ConditionExpression = "attribute_not_exists(#owner)",
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
            var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);

            await PutAsync(_dynamoDbClient, ScoreItemTableName, partitionKey);

            static async Task PutAsync(IAmazonDynamoDB client, string tableName, string partitionKey)
            {
                var request = new PutItemRequest()
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                        [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(ScoreItemDatabaseConstant.ItemIdSummary),
                        [ScoreItemDatabasePropertyNames.Size] = new AttributeValue(){N = "0"},
                    },
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId
                    },
                    ConditionExpression = "attribute_not_exists(#owner)",
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
