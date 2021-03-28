using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// 自分のメタ情報
    /// </summary>
    public class UserMe
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}
