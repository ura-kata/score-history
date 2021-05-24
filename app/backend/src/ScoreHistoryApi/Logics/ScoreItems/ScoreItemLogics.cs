using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.Extensions.Configuration;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemLogics
    {
        private readonly IScoreQuota _scoreQuota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public ScoreItemLogics(IScoreQuota scoreQuota, IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IConfiguration configuration)
        {
            this._scoreQuota = scoreQuota;
            this._dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _configuration = configuration;
        }

        public ScoreItemAdder Adder => new ScoreItemAdder(
            new ScoreItemDatabase(_scoreQuota, _dynamoDbClient, _configuration),
            new ScoreItemStorage(_scoreQuota, _s3Client, _configuration),
            new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreItemInfoGetter InfoGetter =>
            new ScoreItemInfoGetter(new ScoreItemDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreItemDeleter Deleter =>
            new ScoreItemDeleter(new ScoreItemDatabase(_scoreQuota, _dynamoDbClient, _configuration));
    }
}
