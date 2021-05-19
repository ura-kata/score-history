using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 楽譜のスナップショットデータ
    /// </summary>
    public class ScoreSnapshot
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("data")]
        public ScoreData Data { get; set; }
    }
}
