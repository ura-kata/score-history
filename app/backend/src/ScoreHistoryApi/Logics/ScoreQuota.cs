namespace ScoreHistoryApi.Logics
{
    public enum UserType
    {

    }

    public interface IScoreQuota
    {
        public int ScoreCountMax { get; }
        public long OwnerItemMaxSize { get; }
    }

    /// <summary>
    ///
    /// </summary>
    public class ScoreQuota:IScoreQuota
    {
        public int ScoreCountMax { get; } = 10;
        public long OwnerItemMaxSize { get; } = 1024 * 1024 * 500;
    }
}
