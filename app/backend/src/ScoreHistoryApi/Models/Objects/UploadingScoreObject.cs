using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.Objects
{
    /// <summary>
    /// アップロードするオブジェクトのデータ
    /// </summary>
    public class UploadingScoreObject
    {
        [BindProperty(Name = "content")]
        public IFormFile Content { get; set; }
        [BindProperty(Name = "originalName")]
        public string OriginalName { get; set; }
    }
}
