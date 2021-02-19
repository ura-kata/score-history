using System.Text.Json.Serialization;
using PracticeManagerApi.Services.Providers;

namespace PracticeManagerApi.Mock.Controllers.v1
{
    public class CommitRequest
    {
        [JsonPropertyName("parent")]
        public string Parent { get; set; }

        [JsonPropertyName("commits")]
        public CommitObject[] Commits { get; set; }
    }
}
