using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PracticeManagerApi.Controllers.v1
{
    public class UploadContent
    {
        [BindProperty(Name = "content")]
        public IFormFile Content { get; set; }
        [BindProperty(Name = "original_name")]
        public string OriginalName { get; set; }
    }
}
