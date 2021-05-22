namespace ScoreHistoryApi.Logics.ScoreObjectStorages
{
    /// <summary>
    /// <see cref="ItemTypes"/> に関連する追加情報
    /// </summary>
    public abstract class ImagePngExtra: ExtraBase
    {
        /// <summary> サムネイルデータ </summary>
        public Thumbnail Thumbnail { get; set; }
    }
}
