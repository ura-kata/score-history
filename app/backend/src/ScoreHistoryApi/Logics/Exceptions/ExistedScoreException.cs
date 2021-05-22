using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    /// <summary>
    /// すでに楽譜が存在している
    /// </summary>
    public class ExistedScoreException: InvalidOperationException
    {
        public ExistedScoreException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public ExistedScoreException(string message) : base(message)
        {

        }

        public ExistedScoreException(Exception innerException) : base("Existed score.", innerException)
        {

        }
    }
}
