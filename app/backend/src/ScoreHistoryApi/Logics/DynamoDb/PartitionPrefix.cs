namespace ScoreHistoryApi.Logics.DynamoDb
{
    /// <summary>
    /// DynamoDB のパーティションキーのプレフィックス
    /// </summary>
    public static class PartitionPrefix
    {
        public const string Score = "sc:";
        public const string Item = "si:";
    }
}
