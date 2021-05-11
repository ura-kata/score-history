namespace ScoreHistoryApi.Logics.ScoreObjectStorages
{
    /// <summary>
    /// サムネイル情報
    /// </summary>
    public class Thumbnail: ExtraBase
    {
        /// <summary> S3 のオブジェクト名 </summary>
        public string ObjectName { get; set; }

        /// <summary> バイトサイズ </summary>
        public long Size { get; set; }
    }
}
