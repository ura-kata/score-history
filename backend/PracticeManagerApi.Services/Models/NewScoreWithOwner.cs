using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// 新しい Score
    /// </summary>
    public class NewScoreWithOwner
    {
        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
    }
}
