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
        private readonly IScoreCommonLogic _commonLogic;

        public ScoreLogics(IScoreQuota scoreQuota, IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IConfiguration configuration)
        {
            _scoreQuota = scoreQuota;
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _configuration = configuration;
            _commonLogic = new ScoreCommonLogic();
        }

        public Initializer Initializer => new(_dynamoDbClient, _scoreQuota, _configuration, _commonLogic);

        public ScoreCreator Creator => new(_dynamoDbClient, _scoreQuota, _configuration, _commonLogic);

        public ScoreSummaryGetter SummaryGetter => new(_dynamoDbClient, _scoreQuota, _configuration, _commonLogic);

        public ScoreDeleter Deleter => new(_dynamoDbClient, _s3Client, _scoreQuota, _configuration);

        public ScoreTitleSetter TitleSetter => new(_dynamoDbClient, _scoreQuota, _configuration, _commonLogic);

        public ScoreDescriptionSetter DescriptionSetter => new(_dynamoDbClient, _scoreQuota, _configuration, _commonLogic);

        public ScoreSnapshotCreator SnapshotCreator => new(_dynamoDbClient,_s3Client,_scoreQuota,_configuration);

        public ScoreSnapshotSummaryGetter SnapshotSummaryGetter => new(_dynamoDbClient, _configuration);

        public ScoreDetailGetter DetailGetter => new(_dynamoDbClient, _configuration);

        public ScoreSnapshotDetailGetter SnapshotDetailGetter => new(_s3Client, _configuration);

        public ScoreSnapshotRemover SnapshotRemover => new(_dynamoDbClient, _s3Client, _configuration);

        public ScoreAnnotationAdder AnnotationAdder => new(_dynamoDbClient, _scoreQuota, _configuration, _commonLogic);

        public ScoreAnnotationRemover AnnotationRemover => new(_dynamoDbClient, _scoreQuota, _configuration);

        public ScoreAnnotationReplacer AnnotationReplacer => new(_dynamoDbClient, _scoreQuota, _configuration);

        public ScorePageAdder PageAdder => new(_dynamoDbClient, _scoreQuota, _configuration);

        public ScorePageRemover PageRemover => new(_dynamoDbClient, _scoreQuota, _configuration);

        public ScorePageReplacer PageReplacer => new(_dynamoDbClient, _scoreQuota, _configuration);

        public ScoreAccessSetter AccessSetter => new(_dynamoDbClient, _s3Client, _scoreQuota, _configuration, _commonLogic);
    }
}
