using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class AddCommentCommitObject
    {
        public const string CommitType = "add_comment";

        [JsonPropertyName("page")]
        public string Page { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }
}
