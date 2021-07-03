using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSummaryGetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreSummaryGetter(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration)
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
                    $"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");
            ScoreDataTableName = scoreDataTableName;
        }

        public string ScoreDataTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task<ScoreSummary[]> GetScoreSummaries(Guid ownerId)
        {
            var summaries = await GetScoreSummariesAsync(ownerId);

            return summaries.ToArray();
        }


        public async Task<IReadOnlyList<ScoreSummary>> GetScoreSummariesAsync(Guid ownerId)
        {

            return await GetAsync(_dynamoDbClient, ScoreTableName, ScoreDataTableName, ownerId);


            static async Task<ScoreSummary[]> GetAsync(IAmazonDynamoDB client, string tableName, string dataTableName, Guid ownerId)
            {
                var scorePartitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var largeDataPartitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.PartitionKey,
                        ["#score"] = DynamoDbScorePropertyNames.SortKey,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#title"] = DynamoDbScorePropertyNames.DataPropertyNames.Title,
                        ["#desc"] = DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(scorePartitionKey),
                        [":mainPrefix"] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#score, :mainPrefix)",
                    ProjectionExpression = "#owner, #score, #data.#title, #data.#desc",
                };

                var requestData = new QueryRequest()
                {
                    TableName = dataTableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScoreDataPropertyNames.OwnerId,
                        ["#data"] = DynamoDbScoreDataPropertyNames.DataId,
                        ["#content"] = DynamoDbScoreDataPropertyNames.Content,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(largeDataPartitionKey),
                        [":descPrefix"] = new AttributeValue(DynamoDbScoreDataConstant.PrefixDescription),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#data, :descPrefix)",
                    ProjectionExpression = "#data, #content",
                };
                try
                {
                    var response = await client.QueryAsync(request);
                    var responseData = await client.QueryAsync(requestData);

                    var subStartIndex = ScoreDatabaseConstant.ScoreIdMainPrefix.Length;

                    var dataIdSubstringIndex = DynamoDbScoreDataConstant.PrefixDescription.Length;
                    var descriptionSet = responseData.Items.ToDictionary(
                        x => x[DynamoDbScoreDataPropertyNames.DataId].S.Substring(dataIdSubstringIndex),
                        x => x[DynamoDbScoreDataPropertyNames.Content].S);

                    return response.Items
                        .Select(x =>
                        {
                            var ownerId64 = x[DynamoDbScorePropertyNames.PartitionKey].S;
                            var scoreId64 = x[DynamoDbScorePropertyNames.SortKey].S.Substring(subStartIndex);
                            var title = x[DynamoDbScorePropertyNames.Data].M[DynamoDbScorePropertyNames.DataPropertyNames.Title].S;
                            var descriptionHash = x[DynamoDbScorePropertyNames.Data].M[DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash].S;
                            var description = descriptionSet[scoreId64 + descriptionHash];

                            var ownerId = ScoreDatabaseUtils.ConvertFromPartitionKey(ownerId64);
                            var scoreId = ScoreDatabaseUtils.ConvertToGuid(scoreId64);

                            return new ScoreSummary()
                            {
                                Id = scoreId,
                                OwnerId = ownerId,
                                Title = title,
                                Description = description
                            };
                        })
                        .ToArray();
                }
                catch (InternalServerErrorException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ProvisionedThroughputExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (RequestLimitExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ResourceNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
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
