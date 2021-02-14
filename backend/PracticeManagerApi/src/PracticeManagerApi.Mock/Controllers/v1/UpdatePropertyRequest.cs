using System.Text.Json.Serialization;
using PracticeManagerApi.Services.Providers;

namespace PracticeManagerApi.Mock.Controllers.v1
{
    public class UpdatePropertyRequest
    {
        [JsonPropertyName("parent")]
        public string Parent { get; set; }

        [JsonPropertyName("property")]
        public PatchScoreV2Property Property { get; set; }
    }
}
