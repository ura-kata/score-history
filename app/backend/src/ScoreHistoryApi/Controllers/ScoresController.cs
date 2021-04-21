using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("scores")]
    public class ScoresController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public ScoresController(ILogger<ScoresController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        #region user

        [HttpGet]
        [Route("user")]
        public Task<ActionResult<Score[]>> GetUserScoresAsync()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("user")]
        public Task<ActionResult<Score>> CreateUserScoreAsync([FromBody] NewScore newScore)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("user/{id:guid}")]
        public Task<ActionResult<Score>> GetAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }
        
        [HttpDelete]
        [Route("user/{id:guid}")]
        public Task<IActionResult> DeleteAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region owner

        [HttpGet]
        [Route("{owner:guid}")]
        public Task<ActionResult<Score[]>> GetOwnerScoresAsync([FromRoute(Name = "owner")] Guid owner)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("{owner:guid}/{id:guid}")]
        public Task<ActionResult<Score>> GetAOwnerScoreAsync(
            [FromRoute(Name = "owner")] Guid owner,
            [FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}
