using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    public class ScoreSummary
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("owner_id")]
        public Guid OwnerId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
