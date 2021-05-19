#nullable enable

using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    public class NewScoreDescription
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
