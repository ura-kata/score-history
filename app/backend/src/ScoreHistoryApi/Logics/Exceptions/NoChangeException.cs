using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    /// <summary>
    /// 変更がありませんでした
    /// </summary>
    public class NoChangeException : InvalidOperationException
    {
        public NoChangeException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public NoChangeException(string message) : base(message)
        {

        }

        public NoChangeException(Exception innerException) : base("No change.", innerException)
        {

        }

        public NoChangeException() : base("No chnage.")
        {

        }
    }
}
