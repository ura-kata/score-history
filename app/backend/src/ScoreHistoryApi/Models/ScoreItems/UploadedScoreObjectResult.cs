using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// 楽譜のアイテムをアップロードしたときの結果情報
    /// </summary>
    public class UploadedScoreObjectResult
    {
        [JsonPropertyName("itemInfo")]
        public ScoreItemInfo ItemInfo { get; set; }

        [BindProperty(Name = "totalSize")]
        public long TotalSize { get; set; }

        [BindProperty(Name = "emptySize")]
        public long EmptySize { get; set; }
    }
}
