using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreTitleSetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;

        public ScoreTitleSetter(IAmazonDynamoDB dynamoDbClient, IScoreQuota scoreQuota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _scoreQuota = scoreQuota;
            _configuration = configuration;

            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;
        }

        public string ScoreTableName { get; set; }

        public async Task SetTitleAsync(Guid ownerId, Guid scoreId, string title)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var trimTitle = title.Trim();
            if (trimTitle == "")
                throw new ArgumentException(nameof(title));

            var titleMaxLength = _scoreQuota.TitleMaxLength;
            if (titleMaxLength < trimTitle.Length)
                throw new ArgumentException(nameof(title));

            await UpdateTitleAsync(ownerId, scoreId, trimTitle);
        }


        public async Task UpdateTitleAsync(Guid ownerId, Guid scoreId, string title)
        {
            var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, partitionKey, score);

            data.Title = title;

            var newHash = data.CalcDataHash();

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();
            await UpdateAsync(_dynamoDbClient, ScoreTableName, partitionKey, score, title, newHash, oldHash, now);

            static async Task<(DynamoDbScoreDataV1 data,string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string partitionKey,
                string score)
            {
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
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result,hash);
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string partitionKey,
                string score,
                string newTitle,
                string newHash,
                string oldHash,
                DateTimeOffset now
                )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.SortKey] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#title"] = DynamoDbScorePropertyNames.DataPropertyNames.Title,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newTitle"] = new AttributeValue(newTitle),
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression = "SET #updateAt = :updateAt, #hash = :newHash, #data.#title = :newTitle",
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
        }
    }
}
