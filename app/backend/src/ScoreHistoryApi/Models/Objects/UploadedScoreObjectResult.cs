using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.Objects
{
    /// <summary>
    /// オブジェクトをアップロードしたときの結果情報
    /// </summary>
    public class UploadedScoreObjectResult
    {
        [JsonPropertyName("uploadedObjectInfo")]
        public ScoreObjectInfo ObjectInfo { get; set; }

        [BindProperty(Name = "totalSize")]
        public long TotalSize { get; set; }

        [BindProperty(Name = "emptySize")]
        public long EmptySize { get; set; }
    }
}
