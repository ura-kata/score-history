using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class SummaryScoreVersion
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
        [JsonPropertyName(name: "page_count")]
        public int PageCount { get; set; }
    }
}
