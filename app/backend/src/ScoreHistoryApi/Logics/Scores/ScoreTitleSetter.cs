using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.DynamoDb.PropertyNames;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreTitleSetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _scoreQuota;
        private readonly IConfiguration _configuration;
        private readonly IScoreCommonLogic _commonLogic;

        public ScoreTitleSetter(
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

        public async Task SetTitleAsync(Guid ownerId, Guid scoreId, string title)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var trimTitle = title.Trim();
            if (trimTitle == "")
                throw new ArgumentException(nameof(title));

            var titleMaxLength = _scoreQuota.TitleLengthMax;
            if (titleMaxLength < trimTitle.Length)
                throw new ArgumentException(nameof(title));

            await UpdateTitleAsync(ownerId, scoreId, trimTitle);
        }


        public async Task UpdateTitleAsync(Guid ownerId, Guid scoreId, string title)
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
                    ["#title"] = ScoreMainPn.DataPn.Title,
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    [":newTitle"] = new(title),
                    [":newLock"] = new(newLockValue),
                    [":at"] = new(){N = at.ToString()},
                    [":x"] = new() { N = "0" },
                },
                ConditionExpression = "#xt < :at",// TODO API の POST に変更対象の lock の値を付加してその値も比較する
                UpdateExpression = "SET #updateAt = :at, #lock = :newLock, #data.#title = :newTitle, #xs = :x, #xt = :x",
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
