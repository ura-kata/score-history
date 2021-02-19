using System;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Controllers.v1
{
    [Route("api/v1/content")]
    public class ContentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContentController> _logger;
        private readonly IAmazonS3 _s3Client;

        public ContentController(IConfiguration configuration, ILogger<ContentController> logger, IAmazonS3 s3Client)
        {
            _configuration = configuration;
            _logger = logger;
            _s3Client = s3Client;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        [HttpPost("upload")]
        public async Task<UploadedContent> UploadContentsAsync([FromForm]UploadContent content)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteContentsAsync([FromQuery] Uri uri)
        {
            throw new NotImplementedException();
        }
    }
}
