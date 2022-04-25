namespace Db.V1.Names
{
    /// <summary>
    /// 楽譜のアイテムデータのサマリーの Property Name
    /// </summary>
    public class ItemSummaryPn
    {
        /// <summary>si: + user ID</summary>
        public const string PartitionKey = "o";

        /// <summary>summary 固定値</summary>
        public const string SortKey = "s";

        /// <summary>Owner が所有しているアイテムのトータルサイズ</summary>
        public const string TotalSize = "t";

        /// <summary>Owner が所有しているアイテムの数</summary>
        public const string TotalCount = "c";

        /// <summary>作成日時</summary>
        public const string CreateAt = "ca";

        /// <summary>更新日時</summary>
        public const string UpdateAt = "ua";
    }
}
