namespace ScoreHistoryApi.Logics.DynamoDb.PropertyNames
{
    /// <summary>
    /// 楽譜のアノテーションデータの Property Name
    /// </summary>
    public static class ScoreAnnotationPn
    {
        /// <summary>sc: + user ID</summary>
        public const string PartitionKey = "o";
        /// <summary>score ID + :a: + 00 + 0</summary>
        public const string SortKey = "s";

        /// <summary>アノテーションデータ</summary>
        public const string Annotation = "a";
    }
}
