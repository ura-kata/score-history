using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class NewScoreV2Comment
    {
        [JsonPropertyName("target_page")]
        public string TargetPage { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }
}
