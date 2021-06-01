#nullable enable

using System;
using System.Collections.Generic;
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
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("scores")]
    public class ScoresController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ScoreLogics _scoreLogics;

        public ScoresController(ILogger<ScoresController> logger, IConfiguration configuration, ScoreLogics scoreLogics)
        {
            _logger = logger;
            _configuration = configuration;
            _scoreLogics = scoreLogics;
        }

        [HttpPost]
        [Route("new")]
        public async Task ScoreInitializeAsync()
        {
            var authorizerData = this.GetAuthorizerData();

            var initialise = _scoreLogics.Initializer;

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

            var getter = _scoreLogics.SummaryGetter;

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
        public async Task<ActionResult<NewlyScore>> CreateUserScoreAsync([FromBody] NewScore newScore)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var creator = _scoreLogics.Creator;

            try
            {
                return await creator.CreateAsync(ownerId, newScore);
            }
            catch (UninitializedScoreException)
            {
                return StatusCode(ExtensionHttpStatusCodes.NotInitializedScore,
                    new {message = "楽譜を作成するための初期化処理がされていない"});
            }
        }

        /// <summary>
        /// ログインユーザーの単一の楽譜の詳細情報を取得する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet]
        [Route("user/{id:guid}")]
        public async Task<ActionResult<ScoreDetail>> GetAUserScoreAsync([FromRoute(Name = "id")] Guid id)
        {
            var ifNoneMatch = this.Request.Headers[HttpHeaderNames.IfNoneMatch];

            // ETag が If-None-Match と一致する場合は NotModified を返す
            // return this.StatusCode((int) HttpStatusCode.NotModified);

            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var detailGetter = _scoreLogics.DetailGetter;

            try
            {var detail = await detailGetter.GetScoreSummaries(ownerId, id);

                return detail;
            }
            catch (NotFoundScoreException ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(ExtensionHttpStatusCodes.NotFoundScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }


            // this.Response.Headers[HttpHeaderNames.ETag] = "";
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
            var deleter = _scoreLogics.Deleter;

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
        // [HttpPatch]
        // [Route("user/{id:guid}")]
        [NonAction]
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

            var titleSetter = _scoreLogics.TitleSetter;

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

        [HttpPatch]
        [Route("user/{id:guid}/description")]
        public async Task<IActionResult> SetDescriptionAsync([FromRoute(Name = "id")] Guid id, NewScoreDescription description)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var descriptionSetter = _scoreLogics.DescriptionSetter;

            try
            {
                await descriptionSetter.SetDescriptionAsync(ownerId, id, description.Description);
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
        [Route("user/{id:guid}/annotations")]
        public async Task<IActionResult> AddAnnotationsAsync([FromRoute(Name = "id")] Guid id, List<NewScoreAnnotation> annotations)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var annotationsAdder = _scoreLogics.AnnotationAdder;

            try
            {
                await annotationsAdder.AddAnnotations(ownerId, id, annotations);
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

        [HttpDelete]
        [Route("user/{id:guid}/annotations")]
        public async Task<IActionResult> RemoveAnnotationsAsync([FromRoute(Name = "id")] Guid id, List<long> annotationIds)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var annotationsRemover = _scoreLogics.AnnotationRemover;

            try
            {
                await annotationsRemover.RemoveAnnotations(ownerId, id, annotationIds);
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

        [HttpPatch]
        [Route("user/{id:guid}/annotations")]
        public async Task<IActionResult> ReplaceAnnotationsAsync([FromRoute(Name = "id")] Guid id, List<PatchScoreAnnotation> annotations)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var annotationsReplacer = _scoreLogics.AnnotationReplacer;

            try
            {
                await annotationsReplacer.ReplaceAnnotations(ownerId, id, annotations);
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
        [Route("user/{id:guid}/pages")]
        public async Task<IActionResult> AddPagesAsync([FromRoute(Name = "id")] Guid id, List<NewScorePage> pages)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var pageAdder = _scoreLogics.PageAdder;

            try
            {
                await pageAdder.AddPages(ownerId, id, pages);
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

        [HttpDelete]
        [Route("user/{id:guid}/pages")]
        public async Task<IActionResult> RemovePagesAsync([FromRoute(Name = "id")] Guid id, List<long> pageIds)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var pageRemover = _scoreLogics.PageRemover;

            try
            {
                await pageRemover.RemovePages(ownerId, id, pageIds);
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

        [HttpPatch]
        [Route("user/{id:guid}/pages")]
        public async Task<IActionResult> ReplacePagesAsync([FromRoute(Name = "id")] Guid id, List<PatchScorePage> pages)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var pageReplacer = _scoreLogics.PageReplacer;

            try
            {
                await pageReplacer.ReplacePages(ownerId, id, pages);
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

            var snapshotCreator = _scoreLogics.SnapshotCreator;

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

        [HttpGet]
        [Route("user/{id:guid}/snapshots")]
        public async Task<ActionResult<ScoreSnapshotSummary[]>> GetSnapshotSummaryListAsync([FromRoute(Name = "id")] Guid id)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var snapshotSummaryGetter = _scoreLogics.SnapshotSummaryGetter;

            try
            {
                return await snapshotSummaryGetter.GetAsync(ownerId, id);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }

        [HttpGet]
        [Route("user/{id:guid}/snapshots/{snapshotId:guid}")]
        public async Task<ActionResult<ScoreSnapshotDetail>> GetSnapshotDetailAsync([FromRoute(Name = "id")] Guid id,[FromRoute(Name = "snapshotId")]  Guid snapshotId)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var snapshotDetailGetter = _scoreLogics.SnapshotDetailGetter;

            try
            {
                return await snapshotDetailGetter.GetScoreSummaries(ownerId, id, snapshotId);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }

        [HttpDelete]
        [Route("user/{scoreId:guid}/snapshots/{snapshotId:guid}")]
        public async Task<IActionResult> DeleteSnapshotAsync([FromRoute(Name = "scoreId")] Guid scoreId,[FromRoute(Name = "snapshotId")]  Guid snapshotId)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var snapshotRemover = _scoreLogics.SnapshotRemover;

            try
            {
                await snapshotRemover.RemoveAsync(ownerId, scoreId, snapshotId);
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
        }


        [HttpPatch]
        [Route("user/{scoreId:guid}/access")]
        public async Task<IActionResult> SetAccessAsync([FromRoute(Name = "scoreId")] Guid scoreId, PatchScoreAccess access)
        {
            var authorizerData = this.GetAuthorizerData();
            var ownerId = authorizerData.Sub;

            var accessSetter = _scoreLogics.AccessSetter;

            try
            {
                await accessSetter.SetAccessAsync(ownerId, scoreId, access);
            }
            catch (ArgumentNullException)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            catch (ArgumentException)
            {
                return StatusCode(StatusCodes.Status400BadRequest);
            }
            catch (Exception)
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
        public async Task<ActionResult<ScoreSummary[]>> GetOwnerScoresAsync([FromRoute(Name = "owner")] Guid owner)
        {
            var getter = _scoreLogics.SummaryGetter;

            var summaries = await getter.GetScoreSummaries(owner);

            return summaries;
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
        public async Task<ActionResult<ScoreDetail>> GetAOwnerScoreAsync(
            [FromRoute(Name = "owner")] Guid owner,
            [FromRoute(Name = "id")] Guid id)
        {
            var ifNoneMatch = this.Request.Headers[HttpHeaderNames.IfNoneMatch];

            // ETag が If-None-Match と一致する場合は NotModified を返す
            // return this.StatusCode((int) HttpStatusCode.NotModified);

            var detailGetter = _scoreLogics.DetailGetter;

            // TODO 楽譜がないときにエラーコードを返す
            var detail = await detailGetter.GetScoreSummaries(owner, id);

            return detail;

            // this.Response.Headers[HttpHeaderNames.ETag] = "";
        }

        #endregion


    }
}
