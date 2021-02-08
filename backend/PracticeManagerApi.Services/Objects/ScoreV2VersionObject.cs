using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Objects
{
    public class ScoreV2VersionObject : ScoreV2Object
    {
        [JsonPropertyName("property")]
        public ScoreV2PropertyItem Property { get; set; }

        [JsonPropertyName("pages")]
        public string[] Pages { get; set; }

        [JsonPropertyName("parent")]
        public string Parent { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("comments")]
        public Dictionary<string, string[]> Comments { get; set; }
    }
}
