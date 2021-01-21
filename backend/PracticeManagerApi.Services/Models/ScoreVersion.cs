using System;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreVersion
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }
        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
        [JsonPropertyName(name: "create_at")]
        public DateTimeOffset CreateAt { get; set; }
        [JsonPropertyName(name: "update_at")]
        public DateTimeOffset UpdateAt { get; set; }
        [JsonPropertyName(name: "pages")]
        public ScoreVersionPage[] Pages { get; set; }
    }
}
