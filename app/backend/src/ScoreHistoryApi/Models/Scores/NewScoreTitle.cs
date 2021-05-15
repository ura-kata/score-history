using System.Text.Json.Serialization;

#nullable enable

namespace ScoreHistoryApi.Models.Scores
{
    public class NewScoreTitle
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";
    }
}
