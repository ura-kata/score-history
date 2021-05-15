using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    /// <summary>
    /// 楽譜の見初期化
    /// </summary>
    public class UninitializedScoreException: InvalidOperationException
    {
        public UninitializedScoreException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public UninitializedScoreException(string message) : base(message)
        {

        }

        public UninitializedScoreException(Exception innerException) : base("Uninitialized score.", innerException)
        {

        }
    }
}
