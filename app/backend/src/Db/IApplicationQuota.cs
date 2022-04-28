namespace Db
{
    /// <summary>
    /// アプリケーションの制限値
    /// </summary>
    public interface IApplicationQuota
    {
        /// <summary>
        /// 楽譜のリミット数
        /// </summary>
        int ScoreLimit { get; }
    }
}
