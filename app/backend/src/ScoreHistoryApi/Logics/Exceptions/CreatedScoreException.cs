using System;

namespace ScoreHistoryApi.Logics.Exceptions
{
    public enum CreatedScoreExceptionCodes
    {
        /// <summary> 作成上限を超えた </summary>
        ExceededUpperLimit
    }

    /// <summary>
    /// 楽譜作成エラー
    /// </summary>
    public class CreatedScoreException : InvalidOperationException
    {
        public CreatedScoreExceptionCodes Code { get; }

        public CreatedScoreException(CreatedScoreExceptionCodes code, string message, Exception innerException): base(message,innerException)
        {
            Code = code;
        }

        public CreatedScoreException(CreatedScoreExceptionCodes code, string message): base(message)
        {
            Code = code;
        }

        public CreatedScoreException(CreatedScoreExceptionCodes code, Exception innerException): base("Created score.", innerException)
        {
            Code = code;
        }
    }
}
