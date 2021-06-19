using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    public class NewlyScore
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}
