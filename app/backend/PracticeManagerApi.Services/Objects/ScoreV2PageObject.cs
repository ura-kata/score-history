using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Objects
{
    public class ScoreV2PageObject: ScoreV2Object
    {
        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }
    }
}
