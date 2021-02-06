using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Objects
{
    public class ScoreV2PropertyObject: ScoreV2Object
    {
        [JsonPropertyName("parent")]
        public string Parent { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
