using System.Text.Json.Serialization;

#nullable enable

namespace ScoreHistoryApi.Models.Scores
{
    public class NewScore
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
