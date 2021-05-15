using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    /// <summary>
    /// すでに初期化している
    /// </summary>
    public class AlreadyInitializedException: InvalidOperationException
    {
        public AlreadyInitializedException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public AlreadyInitializedException(string message) : base(message)
        {

        }

        public AlreadyInitializedException(Exception innerException) : base("Initializing score.", innerException)
        {

        }
    }
}
