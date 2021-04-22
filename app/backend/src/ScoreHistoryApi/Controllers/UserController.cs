using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Models.Users;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public UserController(ILogger<UserController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        [HttpGet]
        public User Get()
        {

            var auth = this.GetAuthorizerData();

            return new User()
            {
                Email = auth.Email,
                Id = auth.Principalid,
                Username = auth.CognitoUserName
            };
        }
    }
}
