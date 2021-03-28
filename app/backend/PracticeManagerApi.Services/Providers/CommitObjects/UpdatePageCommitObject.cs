using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class UpdatePageCommitObject
    {
        public const string CommitType = "update_page";

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }
    }
}
