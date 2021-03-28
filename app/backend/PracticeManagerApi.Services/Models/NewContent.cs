using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// アップロードするコンテンツの内容
    /// </summary>
    public class NewContent
    {
        [BindProperty(Name = "content")]
        public IFormFile Content { get; set; }

        [BindProperty(Name = "owner")]
        public string Owner { get; set; }

        [BindProperty(Name = "score_name")]
        public string ScoreName { get; set; }
    }
}
