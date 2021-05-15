using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    /// <summary>
    /// 楽譜が存在しない
    /// </summary>
    public class NotFoundScoreException: InvalidOperationException
    {
        public NotFoundScoreException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public NotFoundScoreException(string message) : base(message)
        {

        }

        public NotFoundScoreException(Exception innerException) : base("Not found score.", innerException)
        {

        }

    }
}
