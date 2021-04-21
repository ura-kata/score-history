using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Version
{
    public class ApiVersion
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}
