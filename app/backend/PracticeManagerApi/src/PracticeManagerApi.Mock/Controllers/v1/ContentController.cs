using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Mock.Controllers.v1
{
    [Route("api/v1/content")]
    public class ContentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContentController> _logger;

        public ContentController(IConfiguration configuration, ILogger<ContentController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        [HttpPost("upload")]
        public async Task<UploadedContent> UploadContentsAsync([FromForm]NewContent content)
        {
            var contentsDir = _configuration["ContentsDirectory"];
            var contentName = Guid.NewGuid().ToString("D") + "@" + content.Content.FileName;

            var fileDirPath = Path.Join(contentsDir, content.Owner, content.ScoreName);

            if (false == Directory.Exists(fileDirPath))
            {
                Directory.CreateDirectory(fileDirPath);
            }

            try
            {
                await using var ws = System.IO.File.OpenWrite(Path.Join(fileDirPath, contentName));
                await content.Content.CopyToAsync(ws);
                await ws.FlushAsync();
                ws.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            

            var contentsUrlBase = _configuration["ContentsUrlBase"].TrimEnd('/');

            return new UploadedContent()
            {
                Href = new Uri(contentsUrlBase + "/" + content.Owner + "/" + content.ScoreName + "/" + contentName),
                OriginalName = content.Content.FileName
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete")]
        public IActionResult DeleteContents([FromQuery] Uri uri)
        {
            var url = uri?.ToString();

            if (url == null)
            {
                return BadRequest();
            }

            var contentsUrlBase = _configuration["ContentsUrlBase"].TrimEnd('/') + "/";

            if (url.StartsWith(contentsUrlBase) == false)
            {
                return BadRequest();
            }

            var contentsDir = _configuration["ContentsDirectory"];

            var fileName = url.Substring(contentsUrlBase.Length);

            var filePath = Path.Join(contentsDir, fileName);

            if (System.IO.File.Exists(filePath) == false)
            {
                return NotFound();
            }

            System.IO.File.Delete(filePath);

            return Ok();
        }
    }
}
