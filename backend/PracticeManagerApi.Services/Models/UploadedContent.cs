using System;
using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// アップロードしたコンテンツの情報
    /// </summary>
    public class UploadedContent
    {
        [JsonPropertyName("href")]
        public Uri Href { get; set; }
        [JsonPropertyName("original_name")]
        public string OriginalName { get; set; }
    }
}
