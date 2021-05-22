using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// 楽譜アイテムの情報
    /// </summary>
    public class ScoreItemInfo
    {
        [JsonPropertyName("scoreId")]
        public Guid ScoreId { get; set; }

        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }

        [BindProperty(Name = "originalName")]
        public string OriginalName { get; set; }
    }
}
