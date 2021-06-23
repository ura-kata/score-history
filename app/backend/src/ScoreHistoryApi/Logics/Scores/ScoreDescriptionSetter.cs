using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreDescriptionSetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;

        public ScoreDescriptionSetter(IAmazonDynamoDB dynamoDbClient, IScoreQuota scoreQuota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
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
        }

        public string ScoreDataTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task SetDescriptionAsync(Guid ownerId, Guid scoreId, string description)
        {
            if (description == null)
                throw new ArgumentNullException(nameof(description));

            var trimDescription = description.Trim();

            var titleMaxLength = _scoreQuota.DescriptionMaxLength;
            if (titleMaxLength < trimDescription.Length)
                throw new ArgumentException(nameof(description));

            await UpdateDescriptionAsync(ownerId, scoreId, trimDescription);
        }


        public async Task UpdateDescriptionAsync(Guid ownerId, Guid scoreId, string description)
        {

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            var descriptionHash =
                DynamoDbScoreDataUtils.CalcHash(DynamoDbScoreDataUtils.DescriptionPrefix, description ?? "");

            if (string.Equals(data.DescriptionHash, descriptionHash, StringComparison.InvariantCulture))
            {
                throw new NoChangeException();
            }

            var oldDescriptionHash = data.DescriptionHash;
            data.DescriptionHash = descriptionHash;

            var newHash = data.CalcDataHash();

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();
            await UpdateAsync(
                _dynamoDbClient, ScoreTableName, ScoreDataTableName,
                ownerId, scoreId, descriptionHash, description, oldDescriptionHash,
                newHash, oldHash, now);

            static async Task<(DynamoDbScoreDataV1 data,string hash)> GetAsync(
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
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                if(response.IsItemSet == false)
                    throw new InvalidOperationException("not found.");
                if (!response.Item.TryGetValue(DynamoDbScorePropertyNames.Data, out var data))
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result,hash);
        }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string dataTableName,
                Guid ownerId,
                Guid scoreId,
                string newDescriptionHash,
                string newDescription,
                string oldDescriptionHash,
                string newHash,
                string oldHash,
                DateTimeOffset now
                )
            {
                var scorePartitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var largeDataPartitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);

                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var newDataId = DynamoDbScoreDataConstant.PrefixDescription + score + newDescriptionHash;
                var oldDataId = DynamoDbScoreDataConstant.PrefixDescription + score + oldDescriptionHash;
                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(scorePartitionKey),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                                ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                                ["#data"] = DynamoDbScorePropertyNames.Data,
                                ["#descHash"] = DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":newDescHash"] = new AttributeValue(newDescriptionHash),
                                [":newHash"] = new AttributeValue(newHash),
                                [":oldHash"] = new AttributeValue(oldHash),
                                [":updateAt"] = new AttributeValue(updateAt),
                            },
                            ConditionExpression = "#hash = :oldHash",
                            UpdateExpression = "SET #updateAt = :updateAt, #hash = :newHash, #data.#descHash = :newDescHash",
                            TableName = tableName,
                        }
                    },
                    new TransactWriteItem()
                    {
                        Put = new Put()
                        {
                            TableName = dataTableName,
                            Item = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(largeDataPartitionKey),
                                [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(newDataId),
                                [DynamoDbScoreDataPropertyNames.Content] = new AttributeValue(newDescription),
                            },
                        }
                    },
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            TableName = dataTableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(largeDataPartitionKey),
                                [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(oldDataId),
                            }
                        },
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
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }
    }
}
