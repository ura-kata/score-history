using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Providers
{
    public class PatchScoreV2Property
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
