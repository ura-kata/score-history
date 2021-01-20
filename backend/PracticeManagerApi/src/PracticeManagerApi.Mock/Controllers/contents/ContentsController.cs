using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Mock.Controllers.contents
{
    [Route("api/contents")]
    public class ContentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private ILogger _logger;
        private readonly string _baseDirectory;

        public ContentsController(IConfiguration configuration, ILogger<ContentsController> logger)
        {
            _configuration = configuration;
            this._logger = logger;
            _baseDirectory = "./Contents/";
        }


        [HttpGet]
        [Route("{filename}")]
        public IActionResult GetFile(
            [FromRoute(Name = "filename")] string fileName)
        {
            var filePath = System.IO.Path.Combine(_baseDirectory, fileName);
            if (System.IO.File.Exists(filePath))
            {
                var fileStream = System.IO.File.OpenRead(filePath);

                var extension = System.IO.Path.GetExtension(filePath);
                string contentType = extension.Trim('.').ToLowerInvariant() switch
                {
                    "json" => "application/json",
                    "png" => "image/png",
                    "jpg" => "image/jpeg",
                    "jpeg" => "image/jpeg",
                    _ => "text/plain"
                };
                return File(fileStream, contentType);
            }

            return NotFound();
        }
    }
}
