using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class PatchScoreV2Comment
    {
        [JsonPropertyName("target_page")]
        public string TargetPage { get; set; }

        [JsonPropertyName("target_comment")]
        public string TargetComment { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }
}
