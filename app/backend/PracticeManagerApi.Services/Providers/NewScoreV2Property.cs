using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class NewScoreV2Property
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
