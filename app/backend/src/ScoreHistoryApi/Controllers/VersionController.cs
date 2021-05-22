using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ScoreHistoryApi.Models.Versions;

namespace ScoreHistoryApi.Controllers
{
    [ApiController]
    [Route("version")]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<VersionController> _logger;
        private readonly IConfiguration _configuration;

        public VersionController(ILogger<VersionController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public ApiVersion Get()
        {
            return new ApiVersion()
            {
                Version = _configuration[EnvironmentNames.ApiVersion],
            };
        }
    }
}
