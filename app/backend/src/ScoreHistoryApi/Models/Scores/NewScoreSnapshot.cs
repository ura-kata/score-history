using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 新しい楽譜のスナップショット
    /// </summary>
    public class NewScoreSnapshot
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
