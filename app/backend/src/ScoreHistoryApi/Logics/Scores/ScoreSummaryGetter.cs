using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.DynamoDb;
using ScoreHistoryApi.Logics.DynamoDb.PropertyNames;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSummaryGetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;
        private readonly IScoreCommonLogic _commonLogic;

        public ScoreSummaryGetter(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration, IScoreCommonLogic commonLogic)
        {
            _dynamoDbClient = dynamoDbClient;
            _quota = quota;
            _configuration = configuration;
            _commonLogic = commonLogic;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;
        }

        public string ScoreTableName { get; set; }

        public async Task<ScoreSummary[]> GetScoreSummaries(Guid ownerId)
        {
            var summaries = await GetScoreSummariesAsync(ownerId);

            return summaries.ToArray();
        }


        public async Task<IReadOnlyList<ScoreSummary>> GetScoreSummariesAsync(Guid ownerId)
        {

            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);

            var request = new QueryRequest()
            {
                TableName = ScoreTableName,
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    ["#o"] = ScoreMainPn.PartitionKey,
                    ["#s"] = ScoreMainPn.SortKey,
                    ["#d"] = ScoreMainPn.Data,
                    ["#title"] = ScoreMainPn.DataPn.Title,
                    ["#desc"] = ScoreMainPn.DataPn.Description,
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    [":o"] = new AttributeValue(partitionKey),
                    //[":sum"] = new AttributeValue(DynamoDbConstant.SummarySortKey),
                },
                // TODO DynamoDB のキーの比較では先頭の文字から比較しているっぽいので以下のような比較でも絞り込みができるよう
                // 先頭は必ず f までのアルファベットになるので summary の s より必ず小さくなる
                // 検証を行う
                // KeyConditionExpression = "#o = :o AND #s < :sum",
                KeyConditionExpression = "#o = :o",
                ProjectionExpression = "#s, #d.#title, #d.#desc",
            };

            try
            {
                var response = await _dynamoDbClient.QueryAsync(request);

                return response.Items
                    .Where(x=>x[ScoreMainPn.SortKey].S != DynamoDbConstant.SummarySortKey)
                    .Select(x =>
                    {
                        var scoreId64 = x[ScoreMainPn.SortKey].S;
                        var title = x[ScoreMainPn.Data].M[ScoreMainPn.DataPn.Title].S;
                        var description = x[ScoreMainPn.Data].M[ScoreMainPn.DataPn.Description].S;

                        var scoreId = _commonLogic.ConvertIdFromDynamo(scoreId64);

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
