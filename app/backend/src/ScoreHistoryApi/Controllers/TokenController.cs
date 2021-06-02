using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("token")]
    public class TokenController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public TokenController(ILogger<TokenController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpDelete]
        public IActionResult DeleteTokenAsync()
        {
            var options = new CookieOptions()
            {
                Expires = DateTimeOffset.MinValue,
                Path = "/",
            };
            Response.Cookies.Append("access_token", "deleted", options);
            Response.Cookies.Append("refresh_token", "deleted", options);
            Response.Cookies.Append("id_token", "deleted", options);
            return Ok();
        }
    }
}
