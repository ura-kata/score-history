using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class NewScoreV2Page
    {
        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }
    }
}
