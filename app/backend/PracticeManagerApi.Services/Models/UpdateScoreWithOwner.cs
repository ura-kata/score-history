using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PracticeManagerApi.Services.Providers;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// 更新 Score
    /// </summary>
    public class UpdateScoreWithOwner
    {
        [JsonPropertyName(name: "parent")]
        public string Parent { get; set; }

        [JsonPropertyName(name: "property")]
        public PatchScoreV2Property Property { get; set; }
    }
}
