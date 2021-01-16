using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreVersion
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }
        [JsonPropertyName(name: "pages")]
        public ScoreVersionPage[] Pages { get; set; }
    }
}
