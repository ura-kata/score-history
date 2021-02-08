using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class AddPageCommitObject
    {
        public const string CommitType = "add_page";
        
        [JsonPropertyName("number")]
        public string Number { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; }
    }
}
