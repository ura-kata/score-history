using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Controllers.v1
{
    [Route("api/v1/score_v2")]
    public class ScoreV2Controller : ControllerBase
    {
        IAmazonS3 S3Client { get; set; }
        ILogger Logger { get; set; }

        string BucketName { get; set; }

        public ScoreV2Controller(IConfiguration configuration, ILogger<ScoreV2Controller> logger, IAmazonS3 s3Client)
        {
            this.Logger = logger;
            this.S3Client = s3Client;

            this.BucketName = configuration[Startup.AppS3BucketKey];

            var appUseMinioText = configuration[Startup.AppUseMinioKey];

            var s3Config = (AmazonS3Config)this.S3Client.Config;
            
            s3Config.Timeout = TimeSpan.FromSeconds(10);
            s3Config.ReadWriteTimeout = TimeSpan.FromSeconds(10);
            s3Config.RetryMode = Amazon.Runtime.RequestRetryMode.Standard;
            s3Config.MaxErrorRetry = 1;


            if (string.IsNullOrEmpty(this.BucketName))
            {
                logger.LogCritical(
                    "Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
                throw new Exception(
                    "Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
            }

            logger.LogInformation($"Configured to use bucket {this.BucketName}");
        }

        [HttpGet]
        [Route("{owner}")]
        public IEnumerable<string> GetScoreNameListWithOwner(
            [FromRoute(Name = "owner")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string owner,
            [FromQuery(Name = "q")]
            string q)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("{owner}/{score_name}")]
        public IActionResult CreateScoreWithOwner(
            [FromRoute(Name = "owner")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string owner,
            [FromRoute(Name = "score_name")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string scoreName,
            [FromBody]
            [Required]
            NewScoreWithOwner body)
        {
            throw new NotImplementedException();
        }

        [HttpDelete]
        [Route("{owner}/{score_name}")]
        public IActionResult DeleteScoreWithOwner(
            [FromRoute(Name = "owner")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string owner,
            [FromRoute(Name = "score_name")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string scoreName)
        {
            throw new NotImplementedException();
        }

        [HttpPatch]
        [Route("{owner}/{score_name}")]
        public async Task<IActionResult> UpdateScoreWithOwnerAsync(
            [FromRoute(Name = "owner")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string owner,
            [FromRoute(Name = "score_name")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string scoreName,
            [FromBody]
            [Required]
            UpdateScoreWithOwner body)
        {
            throw new NotImplementedException();
        }
    }
}
