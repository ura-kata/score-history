namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public static class DynamoDbScoreDataConstant
    {
        public const string PrefixAnnotation = "anno:";
        public const string PrefixDescription = "desc:";

        public const int SeparatorLength = 5;

        public const string PartitionKeyPrefix = "ld:";
    }
}
