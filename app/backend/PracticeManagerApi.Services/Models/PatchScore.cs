using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// 更新
    /// </summary>
    public class PatchScore
    {
        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
    }
}
