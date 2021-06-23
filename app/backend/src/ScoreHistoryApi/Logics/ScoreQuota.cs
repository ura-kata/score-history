namespace ScoreHistoryApi.Logics
{
    public enum UserType
    {

    }

    public interface IScoreQuota
    {
        /// <summary>保存可能な全アイテムの上限</summary>
        long OwnerItemMaxSize { get; }

        /// <summary> 楽譜のタイトルの最大長 </summary>
        long TitleMaxLength { get; }

        /// <summary>説明の最大文字数</summary>
        int DescriptionMaxLength { get; }

        /// <summary>楽譜の最大保存数</summary>
        int ScoreCountMax { get; }

        /// <summary>スナップショットの作成最大数</summary>
        int SnapshotCountMax { get; }

        /// <summary>スナップショットの名前の最大文字数</summary>
        int SnapshotNameMaxLength { get; }

        /// <summary>ページの名前の最大文字数</summary>
        int PageNameLength { get; }

        /// <summary>ページの最大数</summary>
        int PageCountLimit { get; }

        /// <summary>アノテーションの最大文字数</summary>
        int AnnotationMaxLength { get; }

        /// <summary>アノテーションの最大文字数</summary>
        int AnnotationChunkMaxLimit { get; }

        /// <summary>アノテーションの作成最大数</summary>
        int AnnotationCountLimit { get; }
    }

    /// <summary>
    ///
    /// </summary>
    public class ScoreQuota:IScoreQuota
    {
        /// <summary>保存可能な全アイテムの上限</summary>
        public long OwnerItemMaxSize { get; } = 1024 * 1024 * 500;

        /// <summary> 楽譜のタイトルの最大長 </summary>
        public long TitleMaxLength { get; } = 64;

        /// <summary>説明の最大文字数</summary>
        public int DescriptionMaxLength { get; } = 1024;



        /// <summary>楽譜の最大保存数</summary>
        public int ScoreCountMax { get; } = 10;

        /// <summary>スナップショットの作成最大数</summary>
        public int SnapshotCountMax { get; } = 100;

        /// <summary>スナップショットの名前の最大文字数</summary>
        public int SnapshotNameMaxLength { get; } = 64;



        /// <summary>ページの名前の最大文字数</summary>
        public int PageNameLength { get; } = 64;

        /// <summary>ページの最大数</summary>
        public int PageCountLimit { get; } = 200;




        /// <summary>アノテーションの最大文字数</summary>
        public int AnnotationMaxLength { get; } = 240;

        /// <summary>アノテーションの最大文字数</summary>
        public int AnnotationChunkMaxLimit { get; } = 200;

        /// <summary>アノテーションの作成最大数</summary>
        public int AnnotationCountLimit { get; } = 10000;
    }
}
