namespace ScoreHistoryApi.Logics.ScoreItemDatabases
{
    /// <summary>
    /// アイテムのサマリーデータの Property Name
    /// </summary>
    public static class ItemSummaryPn
    {
        /// <summary>it: + owner ID</summary>
        public const string PartitionKey = "o";
        /// <summary>固定値 summary</summary>
        public const string SortKey = "s";

        /// <summary>トータルのサイズ</summary>
        public const string TotalSize = "t";
    }

    /// <summary>
    /// アイテムのデータの Property Name
    /// </summary>
    public static class ItemMainPn
    {
        /// <summary>it: + user ID</summary>
        public const string PartitionKey = "o";
        /// <summary>score ID</summary>
        public const string SortKey = "s";

        /// <summary>トータルのサイズ</summary>
        public const string TotalSize = "t";
    }


    public static class ScoreItemDatabasePropertyNames
    {
        public const string OwnerId = "o";
        public const string ItemId = "s";

        public const string ObjName = "obj_name";
        public const string Size = "size";
        public const string TotalSize = "t_size";
        public const string At = "at";
        public const string Type = "type";

        public const string OrgName = "org_name";
        public const string Thumbnail = "thumbnail";
        public static class ThumbnailPropertyNames
        {
            public const string ObjName = "obj_name";
            public const string Size = "size";
        }
    }
}
