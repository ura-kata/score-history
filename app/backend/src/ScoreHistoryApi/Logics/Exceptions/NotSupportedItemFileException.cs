using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    /// <summary>
    /// 楽譜のアイテムとしてサポートされていないファイル
    /// </summary>
    public class NotSupportedItemFileException: InvalidOperationException
    {
        public NotSupportedItemFileException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public NotSupportedItemFileException(string message) : base(message)
        {

        }

        public NotSupportedItemFileException(Exception innerException) : base("Not supported item file.", innerException)
        {

        }
    }
}
