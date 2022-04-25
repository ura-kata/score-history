namespace ScoreHistoryApi.Logics
{
    public enum UserType
    {

    }

    /// <summary>
    /// クオータ
    /// </summary>
    public interface IScoreQuota
    {
        /// <summary>楽譜のタイトルの最大長</summary>
        long TitleLengthMax { get; }

        /// <summary>説明の最大文字数</summary>
        int DescriptionLengthMax { get; }

        /// <summary>楽譜の最大保存数</summary>
        int ScoreCountMax { get; }

        /// <summary>スナップショットの作成最大数</summary>
        int SnapshotCountMax { get; }

        /// <summary>スナップショットの名前の最大文字数</summary>
        int SnapshotNameLengthMax { get; }

        /// <summary>ページの名前の最大文字数</summary>
        int PageNameLengthMax { get; }

        /// <summary>ページの最大数</summary>
        int PageCountMax { get; }

        /// <summary>アノテーションの最大文字数</summary>
        int AnnotationLengthMax { get; }

        /// <summary>アノテーションの作成最大数</summary>
        int AnnotationCountMax { get; }

        /// <summary>保存可能な全アイテムの上限サイズ</summary>
        long OwnerItemMaxSize { get; }

        /// <summary>保存可能な全アイテムの上限数</summary>
        long OwnerItemMaxCount { get; }
    }

    /// <summary>
    ///
    /// </summary>
    public class ScoreQuota : IScoreQuota
    {

        #region 楽譜データ

        /// <summary>楽譜のタイトルの最大長</summary>
        public long TitleLengthMax { get; } = 128;

        /// <summary>説明の最大文字数</summary>
        public int DescriptionLengthMax { get; } = 1024;


        /// <summary>楽譜の最大保存数</summary>
        public int ScoreCountMax { get; } = 10;


        /// <summary>スナップショットの作成最大数</summary>
        public int SnapshotCountMax { get; } = 100;

        /// <summary>スナップショットの名前の最大文字数</summary>
        public int SnapshotNameLengthMax { get; } = 64;


        /// <summary>ページの名前の最大文字数</summary>
        public int PageNameLengthMax { get; } = 64;

        /// <summary>ページの最大数</summary>
        public int PageCountMax { get; } = 200;


        /// <summary>アノテーションの最大文字数</summary>
        public int AnnotationLengthMax { get; } = 240;

        /// <summary>アノテーションの作成最大数</summary>
        public int AnnotationCountMax { get; } = 10000;

        #endregion 楽譜データ

        // -------------------------------------------------------------------------------------------------------------

        #region アイテム

        /// <summary>保存可能な全アイテムの上限サイズ</summary>
        public long OwnerItemMaxSize { get; } = 1024 * 1024 * 500;

        /// <summary>保存可能な全アイテムの上限数</summary>
        public long OwnerItemMaxCount { get; } = 500;

        #endregion アイテム

    }
}
