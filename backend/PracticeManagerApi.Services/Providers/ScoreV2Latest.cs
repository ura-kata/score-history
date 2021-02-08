using System.Text.Json.Serialization;
using PracticeManagerApi.Services.Objects;

namespace PracticeManagerApi.Services.Providers
{
    public class ScoreV2Latest
    {
        [JsonPropertyName("head_hash")]
        public string HeadHash { get; set; }

        [JsonPropertyName("head")]
        public ScoreV2VersionObject Head { get; set; }
    }
}
