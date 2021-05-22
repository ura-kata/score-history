using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 楽譜のアノテーションデータ
    /// </summary>
    public class ScoreAnnotation
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("contentHash")]
        public string ContentHash { get; set; }
    }
}
