using Microsoft.AspNetCore.Http;

namespace PracticeManagerApi.Services.Models
{
    public class NewScoreVersion
    {
        public IFormFileCollection Images { get; set; }
        public string Nos { get; set; }
    }
}
