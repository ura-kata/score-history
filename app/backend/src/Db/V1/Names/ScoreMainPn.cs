namespace Db.V1.Names
{
    /// <summary>
    /// スコアのメインデータの Property Name
    /// </summary>
    public static class ScoreMainPn
    {
        /// <summary>sc: + user ID</summary>
        public const string PartitionKey = "o";

        /// <summary>score ID</summary>
        public const string SortKey = "s";

        /// <summary>作成日時</summary>
        public const string CreateAt = "ca";

        /// <summary>更新日時</summary>
        public const string UpdateAt = "ua";

        /// <summary>アクセスについて</summary>
        public const string Access = "as";

        /// <summary>リソースの特定バージョンの識別子</summary>
        public const string ETag = "e";

        /// <summary>トランザクションスタート</summary>
        public const string TransactionStart = "xs";

        /// <summary>トランザクションタイムアウト</summary>
        public const string TransactionTimeout = "xt";

        /// <summary>データ構造のバージョン</summary>
        public const string Ver = "v";

        /// <summary>スナップショットの数</summary>
        public const string SnapshotCount = "nc";

        /// <summary>スナップショット</summary>
        public const string Snapshot = "n";

        /// <summary>スナップショットの Property Name</summary>
        public static class SnapshotPn
        {
            /// <summary>スナップショットの ID</summary>
            public const string Id = "i";

            /// <summary>スナップショットの名前</summary>
            public const string Name = "n";

            /// <summary>作成日時</summary>
            public const string CreateAt = "ca";
        }

        /// <summary>データ</summary>
        public const string Data = "d";

        /// <summary>Data の Property Name</summary>
        public static class DataPn
        {
            /// <summary>タイトル</summary>
            public const string Title = "t";

            /// <summary>説明</summary>
            public const string Description = "d";

            /// <summary>ページ数</summary>
            public const string PageCount = "pc";

            /// <summary>ページデータ</summary>
            public const string NextPageId = "pi";

            /// <summary>ページデータ</summary>
            public const string Page = "p";

            /// <summary>Page の Property Name</summary>
            public static class PagePn
            {
                /// <summary>Page の ID</summary>
                public const string Id = "i";

                /// <summary>Page のアイテム ID</summary>
                public const string ItemId = "t";

                /// <summary>アイテムオブジェクトの種類</summary>
                public const string Kind = "k";

                /// <summary>Page</summary>
                public const string Name = "p";
            }
            /// <summary>次のアノテーションの id</summary>
            public const string NextAnnotationId = "ai";

            /// <summary>アノテーションの数</summary>
            public const string AnnotationCount = "ac";

            /// <summary>アノテーション</summary>
            public const string Annotation = "a";

            /// <summary>アノテーションのの Property Name</summary>
            public static class AnnotationPn
            {
                /// <summary>アノテーションの ID</summary>
                public const string Id = "i";

                /// <summary>アノテーションデータとの関連 ID</summary>
                public const string RefId = "r";

                /// <summary>アノテーションの長さ</summary>
                public const string Length = "l";
            }
        }
    }
}
