#nullable enable

using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Exceptions;
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

        [HttpPost]
        [Route("new")]
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
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var getter = _scoreLogicFactory.Getter;

            var summaries = await getter.GetScoreSummaries(ownerId);

            return summaries;
        }

        /// <summary>
        /// ログインユーザーの楽譜を作成する
        /// </summary>
        /// <param name="newScore"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        [Route("user")]
        public async Task<IActionResult> CreateUserScoreAsync([FromBody] NewScore newScore)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var creator = _scoreLogicFactory.Creator;

            try
            {
                await creator.CreateAsync(ownerId, newScore);
            }
            catch (UninitializedScoreException)
            {
                return StatusCode(ExtensionHttpStatusCodes.NotInitializedScore,
                    new {message = "楽譜を作成するための初期化処理がされていない"});
            }

            return Ok();
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
        public async Task<IActionResult> DeleteAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;
            var deleter = _scoreLogicFactory.Deleter;

            try
            {
                await deleter.DeleteAsync(ownerId, id);
            }
            catch (NotFoundScoreException)
            {
                return StatusCode(ExtensionHttpStatusCodes.NotFoundScore, new {message = "楽譜が存在しません"});
            }

            return Ok();
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

        [HttpPatch]
        [Route("user/{id:guid}/title")]
        public async Task<IActionResult> SetTitleAsync([FromRoute(Name = "id")] Guid id, NewScoreTitle title)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var titleSetter = _scoreLogicFactory.TitleSetter;

            try
            {
                await titleSetter.SetTitleAsync(ownerId, id, title.Title);
            }
            catch (ArgumentNullException)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            catch (ArgumentException)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            return Ok();

        }

        [HttpPost]
        [Route("user/{id:guid}/snapshots")]
        public async Task<IActionResult> CreateSnapshotAsync([FromRoute(Name = "id")] Guid id, [FromBody] NewScoreSnapshot snapshot)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var snapshotCreator = _scoreLogicFactory.SnapshotCreator;

            try
            {
                await snapshotCreator.CreateAsync(ownerId, id, snapshot.Name);
            }
            catch (ArgumentNullException)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            catch (ArgumentException)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }

            return Ok();
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
