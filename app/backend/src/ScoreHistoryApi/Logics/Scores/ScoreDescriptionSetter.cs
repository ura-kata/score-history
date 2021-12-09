using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.DynamoDb.PropertyNames;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreDescriptionSetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;
        private readonly IScoreCommonLogic _commonLogic;

        public ScoreDescriptionSetter(
            IAmazonDynamoDB dynamoDbClient,
            IScoreQuota scoreQuota,
            IConfiguration configuration,
            IScoreCommonLogic commonLogic
            )
        {
            _dynamoDbClient = dynamoDbClient;
            _scoreQuota = scoreQuota;
            _configuration = configuration;
            _commonLogic = commonLogic;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;
        }


        public string ScoreTableName { get; set; }

        public async Task SetDescriptionAsync(Guid ownerId, Guid scoreId, string description)
        {
            if (description == null)
                throw new ArgumentNullException(nameof(description));

            var trimDescription = description.Trim();

            var titleMaxLength = _scoreQuota.DescriptionLengthMax;
            if (titleMaxLength < trimDescription.Length)
                throw new ArgumentException(nameof(description));

            await UpdateDescriptionAsync(ownerId, scoreId, trimDescription);
        }


        public async Task UpdateDescriptionAsync(Guid ownerId, Guid scoreId, string description)
        {
            var partitionKey = "sc:" + _commonLogic.ConvertIdFromGuid(ownerId);
            var sortKey = _commonLogic.ConvertIdFromGuid(scoreId);


            var newLockValue = _commonLogic.NewLock();
            var now = _commonLogic.Now;
            var at = now.ToUnixTimeMilliseconds();

            var request = new UpdateItemRequest()
            {
                Key = new Dictionary<string, AttributeValue>()
                {
                    [ScoreMainPn.PartitionKey] = new(partitionKey),
                    [ScoreMainPn.SortKey] = new(sortKey),
                },
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    ["#updateAt"] = ScoreMainPn.UpdateAt,
                    ["#lock"] = ScoreMainPn.Lock,
                    ["#xs"] = ScoreMainPn.TransactionStart,
                    ["#xt"] = ScoreMainPn.TransactionTimeout,
                    ["#data"] = ScoreMainPn.Data,
                    ["#desc"] = ScoreMainPn.DataPn.Description,
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    [":newDesc"] = new(description),
                    [":newLock"] = new(newLockValue),
                    [":at"] = new() { N = at.ToString() },
                    [":x"] = new() { N = "0" },
                },
                ConditionExpression = "#xt < :at",// TODO API の POST に変更対象の lock の値を付加してその値も比較する
                UpdateExpression = "SET #updateAt = :at, #lock = :newLock, #data.#desc = :newDesc, #xs = :x, #xt = :x",
                TableName = ScoreTableName,
            };
            try
            {
                await _dynamoDbClient.UpdateItemAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
