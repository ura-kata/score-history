using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("scores")]
    public class ScoresController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ScoreLogicFactory _scoreLogicFactory;

        public ScoresController(ILogger<ScoresController> logger, IConfiguration configuration, ScoreLogicFactory scoreLogicFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scoreLogicFactory = scoreLogicFactory;
        }

        public async Task ScoreInitializeAsync()
        {
            var authorizerData = this.GetAuthorizerData();

            var initialise = _scoreLogicFactory.Initializer;

            var ownerId = authorizerData.Sub;

            await initialise.Initialize(ownerId);
        }

        #region user

        /// <summary>
        /// ログインユーザーがアクセス可能な楽譜の一覧を取得する
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
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

        /// <summary>
        /// ログインユーザーの楽譜を作成する
        /// </summary>
        /// <param name="newScore"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        [Route("user")]
        public async Task<ActionResult<ScoreDetail>> CreateUserScoreAsync([FromBody] NewScore newScore)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// ログインユーザーの単一の楽譜の詳細情報を取得する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet]
        [Route("user/{id:guid}")]
        public Task<ActionResult<ScoreDetail>> GetAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// ログインユーザーの指定された楽譜を削除する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpDelete]
        [Route("user/{id:guid}")]
        public Task<IActionResult> DeleteAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// ログインユーザーの指定された楽譜を更新する
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPatch]
        [Route("user/{id:guid}")]
        public Task<IActionResult> PatchAUserScoreAsync([FromRoute(Name = "id")] Guid id, JsonPatchDocument<ScorePatch> patch)
        {
            var ifMatch = this.Request.Headers[HttpHeaderNames.IfMatch];

            // ETag が If-Match と不一致の場合は PreconditionFailed を返す
            //return this.StatusCode((int)HttpStatusCode.PreconditionFailed);

            throw new NotImplementedException();
        }

        #endregion

        #region owner

        /// <summary>
        /// 指定された owner の所有している楽譜一覧を取得する
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet]
        [Route("{owner:guid}")]
        public Task<ActionResult<ScoreSummary[]>> GetOwnerScoresAsync([FromRoute(Name = "owner")] Guid owner)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定された owner の所有している単一の楽譜の詳細情報を取得する
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
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
