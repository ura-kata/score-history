using System;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreVersionPage
    {
        [JsonPropertyName(name: "image_url")]
        public Uri ImageUrl { get; set; }
        [JsonPropertyName(name: "thumbnail_url")]
        public Uri ThumbnailUrl { get; set; }
        [JsonPropertyName(name: "no")]
        public int No { get; set; }
        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
    }
}
