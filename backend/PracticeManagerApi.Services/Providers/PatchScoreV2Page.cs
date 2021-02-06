using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class PatchScoreV2Page
    {
        [JsonPropertyName("target")]
        public string Target { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }
    }
}
