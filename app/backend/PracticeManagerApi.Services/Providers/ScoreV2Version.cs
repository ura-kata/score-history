using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class ScoreV2Version
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; }

        [JsonPropertyName("version")]
        public int Version { get; set; }
    }
}
