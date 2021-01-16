using System;
using System.Collections.Generic;
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
        private ILogger _logger;

        public ScoreController(IConfiguration configuration, ILogger<ScoreController> logger)
        {
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
            throw new NotImplementedException();
        }

        [HttpGet]
        public async IAsyncEnumerable<Score> GetScores()
        {
            
            throw new NotImplementedException();
            yield break;
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
