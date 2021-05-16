using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    public enum CreatedSnapshotExceptionCodes
    {
        /// <summary> 作成上限を超えた </summary>
        ExceededUpperLimit
    }

    /// <summary>
    /// スナップショット作成エラー
    /// </summary>
    public class CreatedSnapshotException : InvalidOperationException
    {
        public CreatedSnapshotExceptionCodes Code { get; }

        public CreatedSnapshotException(CreatedSnapshotExceptionCodes code, string message, Exception innerException): base(message,innerException)
        {
            Code = code;
        }

        public CreatedSnapshotException(CreatedSnapshotExceptionCodes code, string message): base(message)
        {
            Code = code;
        }

        public CreatedSnapshotException(CreatedSnapshotExceptionCodes code, Exception innerException): base("Created score.", innerException)
        {
            Code = code;
        }
    }
}
