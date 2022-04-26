namespace Db.V1.Names
{
    /// <summary>
    /// 楽譜のアイテムデータの Property Name
    /// </summary>
    public class ItemMainPn
    {
        /// <summary>si: + user ID</summary>
        public const string PartitionKey = "o";

        /// <summary>score ID</summary>
        public const string SortKey = "s";

        /// <summary>作成日時</summary>
        public const string CreateAt = "ca";

        /// <summary>更新日時</summary>
        public const string UpdateAt = "ua";

        /// <summary>トランザクションスタート</summary>
        public const string TransactionStart = "xs";

        /// <summary>トランザクションタイムアウト</summary>
        public const string TransactionTimeout = "xt";

        /// <summary>データ構造のバージョン</summary>
        public const string Ver = "v";

        /// <summary>楽譜に含まれるアイテムのトータルサイズ</summary>
        public const string TotalSizeInScore = "t";

        /// <summary>楽譜に含まれるアイテムの数</summary>
        public const string TotalCountInScore = "c";

        /// <summary>アイテムのリスト</summary>
        public const string Items = "i";

        /// <summary>
        /// アイテムの Property Name
        /// </summary>
        public static class ItemPn
        {
            /// <summary>アイテムの ID</summary>
            public const string Id = "i";

            /// <summary>アイテムのオブジェクトの種類</summary>
            public const string Kind = "k";

            /// <summary>アイテム1つに含まれるデータのトータルサイズ</summary>
            public const string Size = "t";

            /// <summary>アイテムのオリジナル名</summary>
            public const string OriginName = "n";
        }
    }
}
