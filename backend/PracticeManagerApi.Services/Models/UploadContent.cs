using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PracticeManagerApi.Services.Models
{
    /// <summary>
    /// アップロードするコンテンツの内容
    /// </summary>
    public class UploadContent
    {
        [BindProperty(Name = "content")]
        public IFormFile Content { get; set; }
    }
}
