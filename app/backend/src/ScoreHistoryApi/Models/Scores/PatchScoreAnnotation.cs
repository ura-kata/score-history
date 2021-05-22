using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 置き換えるアノテーション
    /// </summary>
    public class PatchScoreAnnotation
    {
        [JsonPropertyName("targetAnnotationId")]
        public int TargetAnnotationId { get; set; }
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
