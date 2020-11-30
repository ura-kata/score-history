using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Model.Internal.MarshallTransformations;
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

            var s3Config = (AmazonS3Config)this.S3Client.Config;

            if (bool.TryParse(appUseMinioText, out var appUseMinio))
            {
                s3Config.ForcePathStyle = appUseMinio;
            }

            s3Config.Timeout = TimeSpan.FromSeconds(10);
            s3Config.ReadWriteTimeout = TimeSpan.FromSeconds(10);
            s3Config.RetryMode = Amazon.Runtime.RequestRetryMode.Standard;
            s3Config.MaxErrorRetry = 3;

            if (string.IsNullOrEmpty(this.BucketName))
            {
                logger.LogCritical("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
                throw new Exception("Missing configuration for S3 bucket. The AppS3Bucket configuration must be set to a S3 bucket.");
            }

            logger.LogInformation($"Configured to use bucket {this.BucketName}");
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
                    CannedACL = S3CannedACL.PublicRead,
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
                var errorMessage = string.Join(Environment.NewLine, new string[]
                {
                    "次のファイルのアップロードに失敗しました"
                }.Concat(uploadErrorFileList.Select(x => $"'{x}'")));
                var errorValue = new Dictionary<string, object>
                {
                    {"message", errorMessage },
                    {"upload_error_file_list", uploadErrorFileList }
                };
                return StatusCode(500, errorValue);

            }

            return Ok();
        }


        [HttpGet]
        [Route("{score_name}/version/{version}")]
        public async Task<ScoreVersion> GetVersion(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromRoute(Name = "version")] int version)
        {
            var prefix = $"{scoreName}/{version}/";
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = prefix,
            };

            try
            {
                var response = await this.S3Client.ListObjectsAsync(request);
                Logger.LogInformation($"List object from bucket {this.BucketName}. Prefix: '{prefix}', Request Id: {response.ResponseMetadata.RequestId}");

                var pages = response.S3Objects.Select(x =>
                new ScoreVersionPage
                {
                    Url = new Uri($"{S3Client.Config.ServiceURL}/{x.BucketName}/{x.Key}"),
                    No = double.Parse(x.Key.Split('/').Last().Split('-')[0]),
                }).ToArray();

                return new ScoreVersion
                {
                    Version = version,
                    Pages = pages,
                };
            }
            catch (AmazonS3Exception e)
            {
                Logger.LogError(e, e.Message);
                throw;
            }
        }

        [HttpGet]
        public async IAsyncEnumerable<Score> GetScores()
        {
            var prefix = $"";
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = prefix,
                Delimiter = "/",
                
            };

            Score[] scores;
            try
            {
                var response = await this.S3Client.ListObjectsAsync(request);
                Logger.LogInformation($"List object from bucket {this.BucketName}. Prefix: '{prefix}', Request Id: {response.ResponseMetadata.RequestId}");

                scores = response.CommonPrefixes.Select(x =>
                    new Score
                    {
                        MetaUrl= new Uri($"{S3Client.Config.ServiceURL}/{BucketName}/{x}{ScoreMeta.FileName}"),
                    }).ToArray();
            }
            catch (AmazonS3Exception e)
            {
                Logger.LogError(e, e.Message);
                throw;
            }

            foreach (var score in scores)
            {
                yield return score;
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateScoreAsync([FromBody] NewScore newScore)
        {
            var convertor = new ScoreMetaConvertor();

            var meta = convertor.Convert(newScore);
            var content = meta.GetLastScoreContent();

            var prefix = $"{content.Name}/{ScoreMeta.FileName}";
            

            try
            {
                var request = new ListObjectsRequest
                {
                    BucketName = BucketName,
                    Prefix = prefix,
                };

                var response = await this.S3Client.ListObjectsAsync(request);
                Logger.LogInformation($"List object from bucket {this.BucketName}. Prefix: '{prefix}', Request Id: {response.ResponseMetadata.RequestId}");

                if (response.S3Objects.Any())
                {
                    throw new InvalidOperationException($"'{content.Name}' は存在します");
                }
            }
            catch (AmazonS3Exception e)
            {
                Logger.LogError(e, e.Message);
                throw;
            }


            try
            {
                var fileName = ScoreMeta.FileName;

                var stream = await convertor.ConvertToUtf(meta);

                var key = $"{content.Name}/{fileName}";
                var putRequest = new PutObjectRequest
                {
                    BucketName = BucketName,
                    Key = key,
                    InputStream = stream,
                    CannedACL = S3CannedACL.PublicRead,
                };
                var response = await this.S3Client.PutObjectAsync(putRequest);
                Logger.LogInformation($"Uploaded object {key} to bucket {this.BucketName}. Request Id: {response.ResponseMetadata.RequestId}");

                return Ok();
            }
            catch (AmazonS3Exception e)
            {
                Logger.LogError(e, e.Message);
                throw;
            }
        }


        [HttpPatch]
        [Route("{score_name}")]
        public async Task<IActionResult> CreateScoreAsync(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromBody] PatchScore patchScore)
        {
            var convertor = new ScoreMetaConvertor();

            var key = $"{scoreName}/{ScoreMeta.FileName}";


            Stream currentStream;
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = BucketName,
                    Key = key,
                };

                var response = await this.S3Client.GetObjectAsync(request);
                Logger.LogInformation(
                    $"List object from bucket {this.BucketName}. Key: '{key}', Request Id: {response.ResponseMetadata.RequestId}");


                currentStream = response.ResponseStream;
            }
            catch (AmazonS3Exception e)
            {
                if (e.ErrorCode == "NoSuchKey")
                {
                    throw new InvalidOperationException($"'{scoreName}' は存在しません");
                }

                Logger.LogError(e, e.Message);
                throw;
            }

            var scoreMeta = await convertor.ConvertToScoreMeta(currentStream);

            var newCurrentScoreMeta = convertor.ConvertToContent(scoreMeta.GetLastScoreContent(), patchScore);

            var newScoreMetaKey = DateTimeOffset.Now.UtcDateTime.ToString("yyyyMMddHHmmssfff");

            scoreMeta[newScoreMetaKey] = newCurrentScoreMeta;

            int retryCount = 5;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    var stream = await convertor.ConvertToUtf(scoreMeta);

                    var putRequest = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key,
                        InputStream = stream,
                        CannedACL = S3CannedACL.PublicRead,
                    };
                    var response = await this.S3Client.PutObjectAsync(putRequest);
                    Logger.LogInformation($"Uploaded object {key} to bucket {this.BucketName}. Request Id: {response.ResponseMetadata.RequestId}");

                }
                catch (AmazonS3Exception e)
                {
                    Logger.LogError(e, e.Message);
                    throw;
                }


                try
                {
                    var request = new GetObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key,
                    };

                    var response = await this.S3Client.GetObjectAsync(request);
                    Logger.LogInformation(
                        $"List object from bucket {this.BucketName}. Key: '{key}', Request Id: {response.ResponseMetadata.RequestId}");


                    var stream = response.ResponseStream;

                    var checkedMeta = await convertor.ConvertToScoreMeta(stream);

                    // 同一のキーが登録される可能性があるがキーには少数3桁の時間を指定してるので
                    // ほぼキーが重なる自体はないと考える
                    // もしキーの重複が問題になる用であれば内容の比較も行う
                    if (checkedMeta.ContainsKey(newScoreMetaKey))
                        return Ok();

                }
                catch (AmazonS3Exception e)
                {
                    Logger.LogError(e, e.Message);
                }

                if(i < (retryCount-1))
                    Thread.Sleep(2000);
            }

            throw new InvalidOperationException("追加に失敗しました");
        }

    }

    public class NewScoreVersion
    {
        public IFormFileCollection Images { get; set; }
        public string Nos { get; set; }
    }

    public class ScoreVersion
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }
        [JsonPropertyName(name: "pages")]
        public ScoreVersionPage[] Pages { get; set; }
    }

    public class ScoreVersionPage
    {
        [JsonPropertyName(name: "url")]
        public Uri Url { get; set; }
        [JsonPropertyName(name: "no")]
        public double No { get; set; }
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

    /// <summary>
    /// 更新
    /// </summary>
    public class PatchScore
    {
        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }
    }

    public class Score
    {
        [JsonPropertyName(name: "meta_rul")]
        public Uri MetaUrl { get; set; }
    }

    public class ScoreMeta: Dictionary<string, ScoreContentMeta>
    {
        public const string FileName = "meta.json";

        public ScoreContentMeta GetLastScoreContent()
        {
            if(this.Count == 0)
                throw new InvalidOperationException("Content が存在しません");

            var maxKey = this.Keys.Aggregate(
                DateTimeOffset.MinValue.UtcDateTime.ToString("yyyyMMddHHmmssfff"),
                (elm, max) => String.CompareOrdinal(max, elm) < 0 ? elm : max);

            return this[maxKey];
        }
    }

    public class ScoreContentMeta
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }

        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "versions")]
        public ScoreVersionMeta[] Versions { get; set; } = new ScoreVersionMeta[0];
    }

    public class ScoreVersionMeta
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "pages")]
        public ScoreVersionPageMeta[] Pages { get; set; } = new ScoreVersionPageMeta[0];
    }

    public class ScoreVersionPageMeta
    {
        [JsonPropertyName(name: "no")]
        public double No { get; set; }

        [JsonPropertyName(name: "prefix")]
        public string Prefix { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "comment_json")]
        public string CommentJson { get; set; }
        
        [JsonPropertyName(name: "overlay_svg")]
        public string OverlaySvg { get; set; }
    }

    public class ScoreMetaConvertor
    {
        public ScoreMeta Convert(NewScore newScore) => Convert(newScore, DateTimeOffset.Now);

        public ScoreMeta Convert(NewScore newScore, DateTimeOffset dateTime)
        {
            var content = new ScoreContentMeta()
            {
                Name = newScore.Name,
                Title = newScore.Title ?? "",
                Description = newScore.Description ?? "",
            };

            return new ScoreMeta()
            {
                {dateTime.UtcDateTime.ToString("yyyyMMddHHmmssfff"), content}
            };
        }

        public ScoreContentMeta ConvertToContent(ScoreContentMeta current, PatchScore patchScore)
        {
            return new ScoreContentMeta()
            {
                Name = current.Name,
                Versions = current.Versions,
                Title = patchScore?.Title ?? current.Title,
                Description = patchScore?.Description ?? current.Description,
            };
        }

        public async Task<Stream> ConvertToUtf(ScoreMeta scoreMeta)
        {
            var memStream = new MemoryStream();
            var option = new JsonSerializerOptions() { };
            await JsonSerializer.SerializeAsync(memStream, scoreMeta, option);
            memStream.Position = 0;
            return memStream;
        }

        public async Task<ScoreMeta> ConvertToScoreMeta(Stream stream)
        {
            var option = new JsonSerializerOptions() { };
            return await JsonSerializer.DeserializeAsync<ScoreMeta>(stream, option);
        }
    }
}
