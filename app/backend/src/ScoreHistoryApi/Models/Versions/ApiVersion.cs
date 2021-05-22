using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Versions
{
    public class ApiVersion
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}
