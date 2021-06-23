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
    public class ScoreSnapshotSummaryGetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IConfiguration _configuration;

        public ScoreSnapshotSummaryGetter(IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _configuration = configuration;

            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;
        }

        public string ScoreTableName { get; set; }

        public async Task<ScoreSnapshotSummary[]> GetAsync(Guid ownerId, Guid scoreId)
        {
            return await GetSnapshotSummariesAsync(ownerId, scoreId);
        }


        public async Task<ScoreSnapshotSummary[]> GetSnapshotSummariesAsync(Guid ownerId,
            Guid scoreId)
        {

            return await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            static async Task<ScoreSnapshotSummary[]> GetAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId,
                        ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                        ["#snapshotName"] = DynamoDbScorePropertyNames.SnapshotName,
                        ["#createAt"] = DynamoDbScorePropertyNames.CreateAt,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                        [":snapPrefix"] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#score, :snapPrefix)",
                    ProjectionExpression = "#score, #snapshotName, #createAt",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    var subStartIndex = (ScoreDatabaseConstant.ScoreIdSnapPrefix + score).Length;

                    return response.Items
                        .Select(x =>(
                                score: x[DynamoDbScorePropertyNames.ScoreId].S,
                                name: x[DynamoDbScorePropertyNames.SnapshotName].S,
                                createAt: x[DynamoDbScorePropertyNames.CreateAt].S)
                        )
                        .Select(x =>
                            new ScoreSnapshotSummary()
                            {
                                Id = ScoreDatabaseUtils.ConvertToGuid(x.score.Substring(subStartIndex)),
                                Name =x.name,
                                CreateAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(x.createAt),
                            }
                        )
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
