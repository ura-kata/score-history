namespace Db.V1.Definitions
{
    /// <summary>
    /// DynamoDB のパーティションキーのプレフィックス
    /// </summary>
    public static class PartitionPrefix
    {
        /// <summary>
        /// 楽譜データのプレフィックス
        /// </summary>
        public const string Score = "sc:";

        /// <summary>
        /// アイテムデータのプレフィックス
        /// </summary>
        public const string Item = "si:";
    }
}
