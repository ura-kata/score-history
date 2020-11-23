using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PracticeManagerApi.Controllers.v1
{
    [Route("api/v1/score")]
    public class ScoreController : ControllerBase
    {
        IAmazonS3 S3Client { get; set; }
        ILogger Logger { get; set; }

        string BucketName { get; set; }

        public ScoreController(IConfiguration configuration, ILogger<S3ProxyController> logger, IAmazonS3 s3Client)
        {
            this.Logger = logger;
            this.S3Client = s3Client;

            this.BucketName = configuration[Startup.AppS3BucketKey];

            var appUseMinioText = configuration[Startup.AppUseMinioKey];

            if (bool.TryParse(appUseMinioText, out var appUseMinio))
            {
                ((AmazonS3Config) this.S3Client.Config).ForcePathStyle = appUseMinio;
            }

            if (string.IsNullOrEmpty(this.BucketName))
            {
                logger.LogCritical("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
                throw new Exception("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
            }

            logger.LogInformation($"Configured to use bucket {this.BucketName}");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewScoreAsync([FromBody] NewScore newScore)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("{score_name}/version/{version}")]
        public async Task<IActionResult> CreateVersion(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromRoute(Name = "version")] int version,
            [FromForm] NewScoreVersion newScoreVersion)
        {
            var images = newScoreVersion.Images;
            var nosText = newScoreVersion.Nos;

            var keyCount = images.GroupBy(x => x.FileName, x => x)
                .Select(x => (filename: x.Key, count: x.Count()))
                .Where(x=>2 <= x.count)
                .ToImmutableArray();

            if (keyCount.Any())
            {
                var errorMessage = string.Join(Environment.NewLine, new string[]
                {
                    "次のファイルが重複しています"
                }.Concat(keyCount.Select(x=>$"'{x.filename}'")));
                throw new InvalidOperationException(errorMessage);
            }

            var nos = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(nosText);

            var notContainsNos = images.Select(x => x.FileName)
                .Where(x => !nos.ContainsKey(x))
                .ToImmutableArray();

            if (notContainsNos.Any())
            {
                var errorMessage = string.Join(Environment.NewLine, new string[]
                {
                    "次のファイルの No が指定されていません"
                }.Concat(notContainsNos.Select(x=>$"'{x}'")));
                throw new InvalidOperationException(errorMessage);
            }

            var uploadErrorFileList = new List<string>();
            foreach (var formFile in images)
            {
                var fileName = formFile.FileName;
                var no = nos[fileName];

                var stream = formFile.OpenReadStream();

                var key = $"{scoreName}/{version}/{no}-{fileName}";
                var putRequest = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = key,
                    InputStream = stream,
                };

                try
                {
                    var response = await this.S3Client.PutObjectAsync(putRequest);
                    Logger.LogInformation($"Uploaded object {key} to bucket {this.BucketName}. Request Id: {response.ResponseMetadata.RequestId}");
                }
                catch (AmazonS3Exception e)
                {
                    Logger.LogError(e, e.Message);
                    uploadErrorFileList.Add(fileName);
                }
            }

            if (uploadErrorFileList.Any())
            {
                var code = 500;
                var errorMessage = string.Join(Environment.NewLine, new string[]
                {
                    "次のファイルのアップロードに失敗しました"
                }.Concat(uploadErrorFileList.Select(x => $"'{x}'")));
                return Problem(detail: errorMessage, statusCode: code);
            }

            return Ok();
        }

        [HttpGet]
        public async IAsyncEnumerable<Score> GetScores()
        {
            yield return new Score();
        }
    }

    public class NewScoreVersion
    {
        public IFormFileCollection Images { get; set; }
        public string Nos { get; set; }
    }

    /// <summary>
    /// 新しい Score
    /// </summary>
    public class NewScore
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }

        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
    }

    public class Score
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }

        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
    }
}
