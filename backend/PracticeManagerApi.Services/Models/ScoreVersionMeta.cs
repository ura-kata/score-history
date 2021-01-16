using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreVersionMeta
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "pages")]
        public Dictionary<string,ScoreVersionPageMeta> Pages { get; set; } = new Dictionary<string, ScoreVersionPageMeta>();
    }
}
