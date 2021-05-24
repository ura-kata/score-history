using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
            catch
            {
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

            var auth = this.GetAuthorizerData();
            var ownerId = auth.Sub;

            var adder = _scoreItemLogics.Adder;
            try
            {
                return await adder.AddAsync(ownerId, uploadingScoreItem);
            }
            catch
            {
                return StatusCode(400);
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
            var auth = this.GetAuthorizerData();
            var ownerId = auth.Sub;

            var deleter = _scoreItemLogics.Deleter;

            try
            {
                await deleter.DeleteItemsAsync(ownerId, deletingScoreItems);
                return Ok();
            }
            catch
            {
                return StatusCode(400);
            }
        }
    }
}
