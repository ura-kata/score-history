using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;


// TODO 変更時にハッシュを確認してから更新するようにする処理を追加する

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜のデータベース
    /// </summary>
    public class ScoreDatabase : IScoreDatabase
    {
        private readonly IScoreQuota _quota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        public string ScoreTableName { get; } = "ura-kata-score-history";
        public string ScoreDataTableName { get; } = "ura-kata-score-history-data";

        public string ScoreItemRelationTableName { get; } = "ura-kata-score-history-item-relation";

        public ScoreDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreDataTableName = configuration[EnvironmentNames.ScoreLargeDataDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreLargeDataDynamoDbTableName}' is not found.");
            ScoreDataTableName = scoreDataTableName;

            var scoreItemRelationDataTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationDataTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");
            ScoreItemRelationTableName = scoreItemRelationDataTableName;

            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }

        public ScoreDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient, string scoreTableName,
            string scoreDataTableName, string scoreItemRelationTableName)
        {
            if (string.IsNullOrWhiteSpace(scoreTableName))
                throw new ArgumentException(nameof(scoreTableName));
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new ArgumentException(nameof(scoreDataTableName));
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new ArgumentException(nameof(scoreItemRelationTableName));

            ScoreTableName = scoreTableName;
            ScoreDataTableName = scoreDataTableName;
            ScoreItemRelationTableName = scoreItemRelationTableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }





        public async Task DeleteSnapshotAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {

            await DeleteItemAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId, snapshotId);

            static async Task DeleteItemAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid scoreId,
                Guid snapshotId
                )
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
                var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshot),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                            },
                            ConditionExpression = "attribute_exists(#score)",
                        },
                    },
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#snapshotCount"] = DynamoDbScorePropertyNames.SnapshotCount,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "-1"},
                            },
                            UpdateExpression = "ADD #snapshotCount :increment",
                        }
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
                catch (TransactionCanceledException ex)
                {
                    var deleteReason = ex.CancellationReasons[0];

                    if (deleteReason.Code == "ConditionalCheckFailed")
                    {
                        throw new NotFoundSnapshotException(ex);
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
