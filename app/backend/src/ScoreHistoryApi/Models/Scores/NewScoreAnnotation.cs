using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 新しいアノテーション
    /// </summary>
    public class NewScoreAnnotation
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
