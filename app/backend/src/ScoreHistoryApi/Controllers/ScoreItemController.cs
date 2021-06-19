using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreItems;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("items")]
    public class ScoreItemController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ScoreItemLogics _scoreItemLogics;

        public ScoreItemController(ILogger<ScoreItemController> logger, IConfiguration configuration, ScoreItemLogics scoreItemLogics)
        {
            _logger = logger;
            _configuration = configuration;
            _scoreItemLogics = scoreItemLogics;
        }


        /// <summary>
        /// ログインユーザーがアップロードしたオブジェクト一覧を取得する
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet]
        [Route("user")]
        public async Task<ActionResult<UserItemsInfoApiResponse>> GetUserObjects()
        {
            var auth = this.GetAuthorizerData();
            var ownerId = auth.Sub;

            var getter = _scoreItemLogics.InfoGetter;

            try
            {
                var itemsInfo = await getter.GetUserItemsInfoAsync(ownerId);
                return new UserItemsInfoApiResponse(itemsInfo);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return StatusCode(400);
            }
        }

        /// <summary>
        /// ログインユーザーのオブジェクトをアップロードする
        /// </summary>
        /// <param name="uploadingScoreItem"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        [Route("user")]
        public async Task<ActionResult<UploadedScoreObjectResult>> UploadObject([FromForm] UploadingScoreItem uploadingScoreItem)
        {
            // File Signature を確認
            _logger.LogDebug("{ScoreId}, {OriginalName}", uploadingScoreItem.ScoreId, uploadingScoreItem.OriginalName);
            _logger.LogDebug("Item: {FileName}, {Name}, {Length}, {Headers}, {ContentDisposition}, {ContentType}",
                uploadingScoreItem.Item.FileName, uploadingScoreItem.Item.Name,
                uploadingScoreItem.Item.Length, uploadingScoreItem.Item.Headers,
                uploadingScoreItem.Item.ContentDisposition, uploadingScoreItem.Item.ContentType);

            var auth = this.GetAuthorizerData();
            var ownerId = auth.Sub;

            var adder = _scoreItemLogics.Adder;
            try
            {
                var response = await adder.AddAsync(ownerId, uploadingScoreItem);

                _logger.LogDebug("success : response : {Response}", response);

                return response;
            }
            catch (NotSupportedItemFileException ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return StatusCode(ExtensionHttpStatusCodes.NotSupportedItemFile, new {message = ex.Message});
            }
        }

        /// <summary>
        /// ログインユーザーの指定されたオブジェクトを削除する
        /// </summary>
        /// <param name="deletingScoreItems"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpDelete]
        [Route("user")]
        public async Task<IActionResult> DeleteObject([FromBody] DeletingScoreItems deletingScoreItems)
        {
            _logger.LogDebug("{ScoreId}, {ItemIds}", deletingScoreItems.ScoreId, deletingScoreItems.ItemIds);

            var auth = this.GetAuthorizerData();
            var ownerId = auth.Sub;

            var deleter = _scoreItemLogics.Deleter;

            try
            {
                await deleter.DeleteItemsAsync(ownerId, deletingScoreItems);
                return Ok();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                return StatusCode(400);
            }
        }
    }
}
