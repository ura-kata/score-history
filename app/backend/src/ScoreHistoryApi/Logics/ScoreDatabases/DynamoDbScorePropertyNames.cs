namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public static class DynamoDbScorePropertyNames
    {
        public const string OwnerId = "owner";
        public const string ScoreId = "score";


        public const string ScoreCount = "score_count";
        public const string Scores = "scores";


        public const string DataHash = "d_hash";
        public const string CreateAt = "create_at";
        public const string UpdateAt = "update_at";
        public const string Access = "access";
        public const string SnapshotCount = "s_count";
        public const string Data = "data";


        public const string SnapshotName = "snapname";
        

        public static class DataPropertyNames
        {
            public const string Title = "title";
            public const string DescriptionHash = "des_h";
            public const string PageCount = "p_count";
            public const string AnnotationCount = "a_count";
            public const string DataVersion = "v";
            public const string Pages = "page";
            public const string Annotations = "anno";

            public static class PagesPropertyNames
            {
                public const string Id = "i";
                public const string ItemId = "it";
                public const string Page = "p";
            }

            public static class AnnotationsPropertyNames
            {
                public const string Id = "i";
                public const string ContentHash = "h";
            }
        }
    }
}
