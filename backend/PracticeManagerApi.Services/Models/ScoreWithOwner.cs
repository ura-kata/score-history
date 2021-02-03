using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// スコア
    /// </summary>
    public class ScoreWithOwner
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }

        [JsonPropertyName(name: "owner")]
        public string Owner { get; set; }

        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
    }
}
