using Amazon.DynamoDBv2;
using Amazon.S3;
using Microsoft.Extensions.Configuration;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreLogics
    {
        private readonly IScoreQuota _scoreQuota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public ScoreLogics(IScoreQuota scoreQuota, IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IConfiguration configuration)
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

        public ScoreSnapshotRemover SnapshotRemover =>
            new ScoreSnapshotRemover(_dynamoDbClient, _s3Client, _configuration);

        public ScoreAnnotationAdder AnnotationAdder =>
            new ScoreAnnotationAdder(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreAnnotationRemover AnnotationRemover =>
            new ScoreAnnotationRemover(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreAnnotationReplacer AnnotationReplacer =>
            new ScoreAnnotationReplacer(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScorePageAdder PageAdder =>
            new ScorePageAdder(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScorePageRemover PageRemover =>
            new ScorePageRemover(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScorePageReplacer PageReplacer =>
            new ScorePageReplacer(new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration));

        public ScoreAccessSetter AccessSetter => new ScoreAccessSetter(
            new ScoreDatabase(_scoreQuota, _dynamoDbClient, _configuration),
            new ScoreItemStorage(_scoreQuota, _s3Client, _configuration),
            new ScoreSnapshotStorage(_s3Client, _configuration));
    }
}
