using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// アップロードする楽譜のアイテムデータ
    /// </summary>
    public class UploadingScoreItem
    {
        [BindProperty(Name = "scoreId")]
        public Guid ScoreId { get; set; }
        [BindProperty(Name = "item")]
        public IFormFile Item { get; set; }
        [BindProperty(Name = "originalName")]
        public string OriginalName { get; set; }
    }
}
