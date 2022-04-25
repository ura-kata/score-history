using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics
{
    public class ScoreItemDatabase : IScoreItemDatabase
    {
        private readonly IScoreQuota _quota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        public string TableName { get; }
        public string ScoreItemRelationTableName { get; }

        public ScoreItemDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            var tableName = configuration[EnvironmentNames.ScoreItemDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemDynamoDbTableName}' is not found.");

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");

            TableName = tableName;
            ScoreItemRelationTableName = scoreItemRelationTableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }
        public ScoreItemDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient,string tableName,string scoreItemRelationTableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new ArgumentException(nameof(scoreItemRelationTableName));

            TableName = tableName;
            ScoreItemRelationTableName = scoreItemRelationTableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }


    }
}
