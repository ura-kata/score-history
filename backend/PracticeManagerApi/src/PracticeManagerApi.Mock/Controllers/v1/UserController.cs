using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Mock.Controllers.v1
{
    /// <summary>
    /// ユーザー情報関連
    /// </summary>
    [Route("api/v1/user")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;

        public UserController(IConfiguration configuration, ILogger<UserController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [Route("me")]
        [HttpGet]
        public async Task<UserMe> GetMe()
        {
            await using var stream = System.IO.File.OpenRead("./MockResponses/v1-UserController-me-Get.json");
            var response = await JsonSerializer.DeserializeAsync<UserMe>(stream);

            return response;
        }
    }
}
