using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class InsertPageCommitObject
    {
        public const string CommitType = "insert_page";

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
