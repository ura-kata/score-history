using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Models.Objects;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("objects")]
    public class ObjectController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public ObjectController(ILogger<ObjectController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }


        /// <summary>
        /// ログインユーザーがアップロードしたオブジェクト一覧を取得する
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet]
        [Route("user")]
        public Task<ActionResult<ScoreObjectInfo[]>>  GetUserObjects()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// ログインユーザーのオブジェクトをアップロードする
        /// </summary>
        /// <param name="uploadingScoreObject"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        [Route("user")]
        public Task<ActionResult<UploadedScoreObjectResult>> UploadObject([FromBody] UploadingScoreObject uploadingScoreObject)
        {
            // File Signature を確認

            throw new NotImplementedException();
        }

        /// <summary>
        /// ログインユーザーの指定されたオブジェクトを削除する
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        [Route("user/{objectId:guid}")]
        public Task<IActionResult> DeleteObject([FromRoute] Guid objectId)
        {
            throw new NotImplementedException();
        }
    }
}
