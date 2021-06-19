namespace ScoreHistoryApi
{
    public static class ExtensionHttpStatusCodes
    {
        /// <summary> 初期化がされていない </summary>
        public const int NotInitializedScore = 520;

        /// <summary> 楽譜が存在しない </summary>
        public const int NotFoundScore = 521;

        /// <summary> 楽譜ではサポートしていないファイル </summary>
        public const int NotSupportedItemFile = 521;
    }
}
