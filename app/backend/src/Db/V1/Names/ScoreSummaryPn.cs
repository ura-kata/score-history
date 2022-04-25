namespace Db.V1.Names
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

        /// <summary>楽譜の保存数</summary>
        public const string ScoreCount = "sc";

        /// <summary>作成日時</summary>
        public const string CreateAt = "ca";

        /// <summary>作成日時</summary>
        public const string UpdateAt = "ua";
    }
}
