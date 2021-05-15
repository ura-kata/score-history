using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;

namespace ScoreHistoryApi.Factories
{
    public class ScoreLogicFactory
    {
        private readonly IScoreQuota _scoreQuota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public ScoreLogicFactory(IScoreQuota scoreQuota, IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IConfiguration configuration)
        {
            _scoreQuota = scoreQuota;
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _configuration = configuration;
        }

        public Initializer Initializer => new Initializer(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration),
            new ScoreItemDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreCreator Creator =>
            new ScoreCreator(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreGetter Getter => new ScoreGetter(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));
    }
}
