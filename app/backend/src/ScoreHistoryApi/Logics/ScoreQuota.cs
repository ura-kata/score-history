namespace ScoreHistoryApi.Logics
{
    public enum UserType
    {

    }

    public interface IScoreQuota
    {
        public int ScoreCountMax { get; }
        public long OwnerItemMaxSize { get; }
        public long TitleMaxLength { get; }
        public int SnapshotCountMax { get; }
    }

    /// <summary>
    ///
    /// </summary>
    public class ScoreQuota:IScoreQuota
    {
        public int ScoreCountMax { get; } = 10;
        public long OwnerItemMaxSize { get; } = 1024 * 1024 * 500;

        /// <summary> 楽譜のタイトルの最大長 </summary>
        /// <remarks>
        /// Unicode の1文字のコード長は 6 byte
        /// Ascii の場合は 1 byte
        /// 最大のサイズは 6 * 64 = 384 byte
        /// </remarks>
        public long TitleMaxLength { get; } = 64;

        public int SnapshotCountMax { get; } = 100;
    }
}
