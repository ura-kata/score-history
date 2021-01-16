using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class Score
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }
        [JsonPropertyName(name: "title")]
        public string Title { get; set; }
        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
        [JsonPropertyName("version_meta_urls")]
        public ScoreVersionMetaUrl[] VersionMetaUrls { get; set; } = new ScoreVersionMetaUrl[0];
    }
}
