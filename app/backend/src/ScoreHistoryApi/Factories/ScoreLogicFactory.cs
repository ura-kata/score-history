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

        public ScoreSummaryGetter SummaryGetter => new ScoreSummaryGetter(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreDeleter Deleter =>
            new ScoreDeleter(
                new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration),
                new ScoreSnapshotStorage(_s3Client, _configuration));

        public ScoreTitleSetter TitleSetter =>
            new ScoreTitleSetter(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration), _scoreQuota);

        public ScoreDescriptionSetter DescriptionSetter =>
            new ScoreDescriptionSetter(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration), _scoreQuota);

        public ScoreSnapshotCreator SnapshotCreator => new ScoreSnapshotCreator(
            new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration),
            new ScoreSnapshotStorage(_s3Client, _configuration));

        public ScoreSnapshotSummaryGetter SnapshotSummaryGetter => new ScoreSnapshotSummaryGetter(
            new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreDetailGetter DetailGetter =>
            new ScoreDetailGetter(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreSnapshotDetailGetter SnapshotDetailGetter =>
            new ScoreSnapshotDetailGetter(new ScoreSnapshotStorage(_s3Client, _configuration));

        public ScoreAnnotationsAdder AnnotationsAdder =>
            new ScoreAnnotationsAdder(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreAnnotationsRemover AnnotationsRemover =>
            new ScoreAnnotationsRemover(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreAnnotationsReplacer AnnotationsReplacer =>
            new ScoreAnnotationsReplacer(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));
    }
}
