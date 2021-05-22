using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Users
{
    public class User
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
    }
}
