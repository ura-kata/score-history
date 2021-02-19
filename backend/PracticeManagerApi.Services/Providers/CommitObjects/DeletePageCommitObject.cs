using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class DeletePageCommitObject
    {
        public const string CommitType = "delete_page";

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
