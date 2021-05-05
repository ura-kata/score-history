namespace ScoreHistoryApi.Logics
{
    public enum UserType
    {

    }

    public interface IScoreQuota
    {
        public int ScoreCountMax { get; }
    }

    /// <summary>
    ///
    /// </summary>
    public class ScoreQuota:IScoreQuota
    {
        public int ScoreCountMax { get; } = 10;
    }
}
