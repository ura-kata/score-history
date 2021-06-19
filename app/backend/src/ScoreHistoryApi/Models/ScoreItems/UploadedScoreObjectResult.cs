using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// 楽譜のアイテムをアップロードしたときの結果情報
    /// </summary>
    public record UploadedScoreObjectResult
    {
        [JsonPropertyName("itemInfo")]
        public ScoreImageItemInfo ImageItemInfo { get; set; }
    }
}
