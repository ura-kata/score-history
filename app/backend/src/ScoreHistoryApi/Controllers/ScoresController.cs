using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        public async Task<ActionResult<ScoreSummary[]>> GetUserScoresAsync()
        {
            var ifNoneMatch = this.Request.Headers[HttpHeaderNames.IfNoneMatch];

            // ETag が If-None-Match と一致する場合は NotModified を返す
            // return this.StatusCode((int) HttpStatusCode.NotModified);


            this.Response.Headers[HttpHeaderNames.ETag] = "";
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("user")]
        public async Task<ActionResult<ScoreDetail>> CreateUserScoreAsync([FromBody] NewScore newScore)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("user/{id:guid}")]
        public Task<ActionResult<ScoreDetail>> GetAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }
        
        [HttpDelete]
        [Route("user/{id:guid}")]
        public Task<IActionResult> DeleteAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }

        [HttpPatch]
        [Route("user/{id:guid}")]
        public Task<IActionResult> PatchAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            var ifMatch = this.Request.Headers[HttpHeaderNames.IfMatch];

            // ETag が If-Match と不一致の場合は PreconditionFailed を返す
            //return this.StatusCode((int)HttpStatusCode.PreconditionFailed);

            throw new NotImplementedException();
        }

        #endregion

        #region owner

        [HttpGet]
        [Route("{owner:guid}")]
        public Task<ActionResult<ScoreSummary[]>> GetOwnerScoresAsync([FromRoute(Name = "owner")] Guid owner)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("{owner:guid}/{id:guid}")]
        public Task<ActionResult<ScoreDetail>> GetAOwnerScoreAsync(
            [FromRoute(Name = "owner")] Guid owner,
            [FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}
