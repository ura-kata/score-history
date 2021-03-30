using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Mock.Controllers.v1
{
    [Route("api/v1/score")]
    public class ScoreController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private ILogger _logger;

        public ScoreController(IConfiguration configuration, ILogger<ScoreController> logger)
        {
            _configuration = configuration;
            this._logger = logger;
        }


        [HttpPost]
        [Route("{score_name}/version")]
        public async Task<IActionResult> CreateVersion(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromForm] NewScoreVersion newScoreVersion)
        {
            throw new NotImplementedException();
        }


        [HttpGet]
        [Route("{score_name}/version/{version}")]
        public async Task<ScoreVersion> GetVersion(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromRoute(Name = "version")] int version)
        {
            var responseFilePathTemplate = _configuration["Response:v1:score:score_name:version:version:GET_template"];
            var contentsUrlBase = _configuration["ContentsUrlBase"].TrimEnd('/');

            var filePath = responseFilePathTemplate
                .Replace("${score_name}", scoreName)
                .Replace("${version}", $"{version}");

            if (System.IO.File.Exists(filePath) == false)
            {
                throw new InvalidOperationException($"score_name: '{scoreName}', version: {version} は存在しません");
            }

            var jsonText = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var jsonTextResult = jsonText.Replace("${ContentsUrlBase}", contentsUrlBase);

            await using var memoryStream = new MemoryStream();
            await using var sw = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

            await sw.WriteAsync(jsonTextResult);
            await sw.FlushAsync();

            memoryStream.Seek(0, SeekOrigin.Begin);

            var scoreVersion = await JsonSerializer.DeserializeAsync<ScoreVersion>(memoryStream);

            return scoreVersion;
        }

        [HttpGet]
        public async IAsyncEnumerable<Score> GetScores()
        {
            var contentsUrlBase = _configuration["ContentsUrlBase"].TrimEnd('/');
            var filePath = _configuration["Response:v1:score:GET"];
            if(string.IsNullOrWhiteSpace(filePath))
                yield break;


            var jsonText = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var jsonTextResult = jsonText.Replace("${ContentsUrlBase}", contentsUrlBase);

            await using var memoryStream = new MemoryStream();
            await using var sw = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

            await sw.WriteAsync(jsonTextResult);
            await sw.FlushAsync();

            memoryStream.Seek(0, SeekOrigin.Begin);

            var request = await JsonSerializer.DeserializeAsync<Score[]>(memoryStream);

            if (request == null)
                yield break;

            foreach (var score in request)
            {
                yield return score;
            }
        }


        [HttpGet]
        [Route("{score_name}")]
        public async Task<Score> GetScore(
            [FromRoute(Name = "score_name")] string scoreName)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<IActionResult> CreateScoreAsync([FromBody] NewScore newScore)
        {
            throw new NotImplementedException();
        }



        [HttpPatch]
        [Route("{score_name}")]
        public async Task<IActionResult> CreateScoreAsync(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromBody] PatchScore patchScore)
        {
            throw new NotImplementedException();
        }

    }
}
