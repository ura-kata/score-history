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
    public class ScoreDetailGetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IConfiguration _configuration;

        public ScoreDetailGetter(IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
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

        public async Task<ScoreDetail> GetScoreSummaries(Guid ownerId, Guid scoreId)
        {
            var (data, hashSet) = await GetDynamoDbScoreDataAsync(ownerId, scoreId);

            return ScoreDetail.Create(data, hashSet);
        }


        public async Task<(DynamoDbScore score, Dictionary<string, string> hashSet)> GetDynamoDbScoreDataAsync(
            Guid ownerId, Guid scoreId)
        {

            var dynamoDbScore = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);
            var hashSet = await GetAnnotationsAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId);

            var descriptionHash = dynamoDbScore.Data.GetDescriptionHash();
            var (success, description) =
                await TryGetDescriptionAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId, descriptionHash);

            if (success)
            {
                hashSet[descriptionHash] = description;
            }

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
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);

                if (!response.IsItemSet)
                {
                    throw new NotFoundScoreException("Not found score.");
                }

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
        }
    }
}
