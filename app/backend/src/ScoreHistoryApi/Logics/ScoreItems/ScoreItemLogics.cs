using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemLogics
    {
        private readonly IScoreQuota _scoreQuota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IConfiguration _configuration;

        public ScoreItemLogics(IScoreQuota scoreQuota, IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            this._scoreQuota = scoreQuota;
            this._dynamoDbClient = dynamoDbClient;
            _configuration = configuration;
        }

        public ScoreItemAdder Adder => new ScoreItemAdder();

        public ScoreItemInfoGetter InfoGetter =>
            new ScoreItemInfoGetter(new ScoreItemDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreItemDeleter Deleter => new ScoreItemDeleter();
    }
}
