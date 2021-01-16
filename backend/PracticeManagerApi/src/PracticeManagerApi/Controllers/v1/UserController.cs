using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Controllers.v1
{
    /// <summary>
    /// ユーザー情報関連
    /// </summary>
    [Route("api/v1/user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(IConfiguration configuration, ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [Route("me")]
        [HttpGet]
        public UserMe GetMe()
        {
            string id = null;
            string name = null;
            string email = null;

            foreach (var claim in Request.HttpContext.User.Claims)
            {
                switch (claim.Type.ToLowerInvariant())
                {
                    case "principalid":
                        id = claim.Value;
                        break;
                    case "email":
                        email = claim.Value;
                        break;
                    case "cognito:username":
                        name = claim.Value;
                        break;
                }
            }

            return new UserMe()
            {
                Name = name,
                Id = id,
                Email = email
            };
        }
    }
}
