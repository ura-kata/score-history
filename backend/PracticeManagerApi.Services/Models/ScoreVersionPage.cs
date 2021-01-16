using System;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreVersionPage
    {
        [JsonPropertyName(name: "url")]
        public Uri Url { get; set; }
        [JsonPropertyName(name: "no")]
        public double No { get; set; }
    }
}
