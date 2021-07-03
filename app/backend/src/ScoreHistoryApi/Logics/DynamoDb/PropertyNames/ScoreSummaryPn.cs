namespace ScoreHistoryApi.Logics.DynamoDb.PropertyNames
{
    /// <summary>
    /// 楽譜のサマリーデータの Property Name
    /// </summary>
    public static class ScoreSummaryPn
    {
        /// <summary>sc: + user ID</summary>
        public const string PartitionKey = "o";
        /// <summary>固定値 summary</summary>
        public const string SortKey = "s";

        /// <summary>楽譜の数</summary>
        public const string ScoreCount = "sc";
    }
}
