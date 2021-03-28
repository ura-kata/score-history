using System;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreVersionMetaUrl
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "url")]
        public Uri Url { get; set; }
    }
}
