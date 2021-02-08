using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class UpdatePropertyCommitObject
    {
        public const string CommitType = "update_property";

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
