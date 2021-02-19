using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Objects
{
    public class ScoreV2PropertyItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
