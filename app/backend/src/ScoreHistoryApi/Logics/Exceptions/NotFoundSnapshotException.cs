using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    /// <summary>
    /// スナップショットが存在しない
    /// </summary>
    public class NotFoundSnapshotException : InvalidOperationException
    {
        public NotFoundSnapshotException(string message, Exception innerException) : base(message, innerException)
        {

        }

        public NotFoundSnapshotException(string message) : base(message)
        {

        }

        public NotFoundSnapshotException(Exception innerException) : base("Not found snapshot.", innerException)
        {

        }

    }
}
