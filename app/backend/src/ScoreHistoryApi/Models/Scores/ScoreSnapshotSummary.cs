using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// スナップショットのサマリー
    /// </summary>
    public class ScoreSnapshotSummary
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("createAt")]
        public DateTimeOffset CreateAt { get; set; }
    }
}
