namespace Db.V1.Names
{
    /// <summary>
    /// 楽譜のアノテーションデータの Property Name
    /// </summary>
    public static class AnnotationDataPn
    {
        /// <summary>sc: + user ID</summary>
        public const string PartitionKey = "o";

        /// <summary>score ID + :a: + 0 + 00</summary>
        public const string SortKey = "s";

        /// <summary>アノテーションデータ</summary>
        public const string Annotation = "a";
    }
}
