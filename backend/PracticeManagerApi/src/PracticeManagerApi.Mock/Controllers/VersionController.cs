using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace PracticeManagerApi.Mock.Controllers
{
    [Route("api/version")]
    public class VersionController : ControllerBase
    {
        /// <summary>
        /// バージョンの取得
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetVersion()
        {
            var version = new Dictionary<string, string>()
            {
                { "version", "mock"},
            };

            return Ok(version);

        }
    }
}
