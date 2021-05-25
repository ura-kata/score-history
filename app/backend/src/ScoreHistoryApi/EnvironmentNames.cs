namespace ScoreHistoryApi
{
    public static class EnvironmentNames
    {
        public const string CorsOrigins = "CorsOrigins";
        public const string CorsHeaders = "CorsHeaders";
        public const string CorsMethods = "CorsMethods";
        public const string CorsCredentials = "CorsCredentials";
        public const string ApiVersion = "ApiVersion";

        public const string ScoreItemDynamoDbTableName = "ScoreItemDynamoDbTableName";
        public const string ScoreDynamoDbTableName  = "ScoreDynamoDbTableName";
        public const string ScoreLargeDataDynamoDbTableName = "ScoreLargeDataDynamoDbTableName";

        public const string ScoreItemS3Bucket = "ScoreItemS3Bucket";
        public const string ScoreDataSnapshotS3Bucket = "ScoreDataSnapshotS3Bucket";

        public const string ScoreDynamoDbRegionSystemName = "ScoreDynamoDbRegionSystemName";
        public const string ScoreS3RegionSystemName = "ScoreS3RegionSystemName";

        public const string ScoreDynamoDbEndpointUrl = "ScoreDynamoDbEndpointUrl";
        public const string ScoreS3EndpointUrl = "ScoreS3EndpointUrl";
        public const string ScoreS3AccessKey = "ScoreS3AccessKey";
        public const string ScoreS3SecretKey = "ScoreS3SecretKey";

        public const string DevelopmentSub = "DevelopmentSub";
        public const string DevelopmentPrincipalId = "DevelopmentPrincipalId";
        public const string DevelopmentCognitoUsername = "DevelopmentCognitoUsername";
        public const string DevelopmentEmail = "DevelopmentEmail";
    }
}
