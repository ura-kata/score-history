using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PracticeManagerApi.Controllers
{
    [Route("api/sample")]
    public class SampleController : ControllerBase
    {
        private readonly ILogger<SampleController> _logger;

        public SampleController(ILogger<SampleController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// バージョンの取得
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetVersion()
        {
            var claims = Request.HttpContext.User.Claims?.Select(x=>new {x.Value, x.Type, x.ValueType, x.Issuer}).ToArray();
            
            return Ok(new {claims= claims });

        }
    }
}
