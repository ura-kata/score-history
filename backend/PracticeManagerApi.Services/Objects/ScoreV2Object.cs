using System;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Objects
{
    public abstract class ScoreV2Object
    {
        [JsonPropertyName("create_at")]
        public DateTimeOffset CreateAt { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }
    }
}
