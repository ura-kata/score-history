using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class DeleteCommentCommitObject
    {
        public const string CommitType = "delete_comment";

        [JsonPropertyName("page")]
        public string Page { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
