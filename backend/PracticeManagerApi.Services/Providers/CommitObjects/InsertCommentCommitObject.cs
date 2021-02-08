using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class InsertCommentCommitObject
    {
        public const string CommitType = "insert_comment";

        [JsonPropertyName("page")]
        public string Page { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }
}
