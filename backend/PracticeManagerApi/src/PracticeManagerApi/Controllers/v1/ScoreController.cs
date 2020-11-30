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
        [Route("{score_name}/version")]
        public async Task<IActionResult> CreateVersion(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromForm] NewScoreVersion newScoreVersion)
        {
            var metaFileOperator = new MetaFileOperator(S3Client, BucketName);

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

            var nos = JsonSerializer.Deserialize<Dictionary<string, double>>(nosText);

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

            var now = DateTimeOffset.UtcNow;

            var nextVersion = await metaFileOperator.NewVersionNumber(scoreName);

            var versionFileKey = metaFileOperator.CreateVersionFileKey(scoreName, nextVersion, now);
            
            var versionMeta = new ScoreVersionMeta()
            {
                Version = int.Parse(nextVersion),
            };
            foreach (var formFile in images)
            {
                var no = nos[formFile.FileName].ToString("F").TrimEnd('0').TrimEnd('.');

                var commentPrefix = metaFileOperator.CreatePageCommentPrefix(scoreName, nextVersion, now, no);
                try
                {
                    var key = await metaFileOperator.SaveImage(scoreName, formFile);

                    versionMeta.Pages[no] = new ScoreVersionPageMeta()
                    {
                        No = no,
                        ImageFileKey = key,
                        CommentPrefix = commentPrefix,
                    };
                }
                catch (AmazonS3Exception e)
                {
                    Logger.LogError(e, e.Message);
                    throw;
                }
            }

            await metaFileOperator.SaveVersionMetaAsync(versionFileKey, versionMeta);

            var scoreMeta = await metaFileOperator.GetScoreMetaAsync(scoreName);

            var scoreContentMeta = await scoreMeta.GetLastScoreContent().DeepCopyAsync();

            scoreContentMeta.VersionFileKeys[nextVersion] = versionFileKey;

            var scoreMetaKey = ScoreMeta.CreateKey();

            scoreMeta[scoreMetaKey] = scoreContentMeta;

            await metaFileOperator.SaveScoreMetaAsync(scoreName, scoreMeta, scoreMetaKey);

            return Ok();
        }


        [HttpGet]
        [Route("{score_name}/version/{version}")]
        public async Task<ScoreVersion> GetVersion(
            [FromRoute(Name = "score_name")] string scoreName,
            [FromRoute(Name = "version")] int version)
        {
            var metaFileOperator = new MetaFileOperator(S3Client, BucketName);

            var scoreMeta = await metaFileOperator.GetScoreMetaAsync(scoreName);

            var scoreContentMeta = scoreMeta.GetLastScoreContent();

            var versionText = version.ToString("00000");

            if(scoreContentMeta.VersionFileKeys.TryGetValue(versionText, out var versionFileKey) == false)
                throw new InvalidOperationException($"'{scoreName}' にバージョン '{version}' は存在しません");

            var versionMeta = await metaFileOperator.GetVersionMetaAsync(versionFileKey);

            var urlConvertor = new MinioUrlConvertor(S3Client.Config.ServiceURL, BucketName);

            var metaConvertor = new ScoreMetaConvertor();

            var scoreVersion = metaConvertor.ConvertTo(versionMeta, urlConvertor);

            return scoreVersion;
        }

        [HttpGet]
        public async IAsyncEnumerable<Score> GetScores()
        {
            var metaFileOperator = new MetaFileOperator(S3Client, BucketName);

            var scoreNames = await metaFileOperator.GetScoreNamesAsync();

            var urlConvertor = new MinioUrlConvertor(S3Client.Config.ServiceURL, BucketName);


            var scores = scoreNames.Select(x => new Score()
            {
                MetaUrl = urlConvertor.CreateUri($"{x}/meta.json")
            });

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
            var metaFileOperator = new MetaFileOperator(S3Client, BucketName);

            var key = $"{scoreName}/{ScoreMeta.FileName}";


            ScoreMeta scoreMeta;
            try
            {
                scoreMeta = await metaFileOperator.GetScoreMetaAsync(scoreName);
            }
            catch (AmazonS3Exception e)
            {
                Logger.LogError(e, e.Message);
                throw;
            }

            var newCurrentScoreMeta = convertor.ConvertToContent(scoreMeta.GetLastScoreContent(), patchScore);

            var newScoreMetaKey = ScoreMeta.CreateKey();

            scoreMeta[newScoreMetaKey] = newCurrentScoreMeta;

            try
            {
                await metaFileOperator.SaveScoreMetaAsync(scoreName, scoreMeta, newScoreMetaKey);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                throw;
            }

            return Ok();
        }

    }

    public class MetaFileOperator
    {
        public IAmazonS3 S3Client { get; }
        public string BucketName { get; }

        public MetaFileOperator(IAmazonS3 s3Client, string bucketName)
        {
            S3Client = s3Client;
            BucketName = bucketName;
        }

        public async Task<string[]> GetScoreNamesAsync()
        {
            var prefix = $"";
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = prefix,
                Delimiter = "/",
            };

            try
            {
                var response = await this.S3Client.ListObjectsAsync(request);

                return response.CommonPrefixes.Select(x=>x.TrimEnd('/')).ToArray();

            }
            catch (AmazonS3Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }

        public async Task<ScoreMeta> GetScoreMetaAsync(string scoreName)
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

                currentStream = response.ResponseStream;
            }
            catch (AmazonS3Exception e)
            {
                if (e.ErrorCode == "NoSuchKey")
                {
                    throw new InvalidOperationException($"'{scoreName}' は存在しません");
                }

                throw;
            }

            var scoreMeta = await convertor.ConvertToScoreMeta(currentStream);
            return scoreMeta;
        }

        public async Task SaveScoreMetaAsync(string scoreName, ScoreMeta scoreMeta, string metaKey)
        {
            var convertor = new ScoreMetaConvertor();
            var key = $"{scoreName}/{ScoreMeta.FileName}";

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
                    await this.S3Client.PutObjectAsync(putRequest);

                }
                catch (AmazonS3Exception)
                {
                    // Error
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

                    var stream = response.ResponseStream;

                    var checkedMeta = await convertor.ConvertToScoreMeta(stream);

                    // 同一のキーが登録される可能性があるがキーには少数3桁の時間を指定してるので
                    // ほぼキーが重なる自体はないと考える
                    // もしキーの重複が問題になる用であれば内容の比較も行う
                    if (checkedMeta.ContainsKey(metaKey))
                        return;

                }
                catch (AmazonS3Exception)
                {
                    // skip
                }

                if (i < (retryCount - 1))
                    Thread.Sleep(2000);
            }

            throw new InvalidOperationException("追加更新に失敗しました");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scoreName"></param>
        /// <param name="image"></param>
        /// <returns>Key</returns>
        public async Task<string> SaveImage(string scoreName, IFormFile image)
        {
            var ext = System.IO.Path.GetExtension(image.FileName);

            var key = $"{scoreName}/images/{Guid.NewGuid():N}{ext}";

            var stream = image.OpenReadStream();
            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = key,
                InputStream = stream,
                CannedACL = S3CannedACL.PublicRead,
            };

            try
            {
                await this.S3Client.PutObjectAsync(putRequest);
            }
            catch (AmazonS3Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }

            return key;
        }

        public async Task<string> SaveVersionMetaAsync(string versionFileKey, ScoreVersionMeta versionMeta)
        {
            var metaConvertor = new ScoreMetaConvertor();

            var stream = await metaConvertor.ConvertToUtf(versionMeta);
            var putRequest = new PutObjectRequest
            {
                BucketName = BucketName,
                Key = versionFileKey,
                InputStream = stream,
                CannedACL = S3CannedACL.PublicRead,
            };

            try
            {
                await this.S3Client.PutObjectAsync(putRequest);
            }
            catch (AmazonS3Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }

            return versionFileKey;
        }

        public async Task<ScoreVersionMeta> GetVersionMetaAsync(string versionFileKey)
        {
            var convertor = new ScoreMetaConvertor();

            Stream currentStream;
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = BucketName,
                    Key = versionFileKey,
                };

                var response = await this.S3Client.GetObjectAsync(request);

                currentStream = response.ResponseStream;
            }
            catch (AmazonS3Exception e)
            {
                if (e.ErrorCode == "NoSuchKey")
                {
                    throw new InvalidOperationException($"'{versionFileKey}' は存在しません");
                }

                throw;
            }

            var meta = await convertor.ConvertToScoreVersionMeta(currentStream);
            return meta;
        }

        public async Task<string> NewVersionNumber(string scoreName)
        {
            var prefix = $"{scoreName}/versions/";
            var request = new ListObjectsRequest
            {
                BucketName = BucketName,
                Prefix = prefix,
                Delimiter = "/",
            };

            try
            {
                var response = await this.S3Client.ListObjectsAsync(request);

                var nextVersion =
                    response.CommonPrefixes.Count switch
                    {
                        0 => 0,
                        _ => response.CommonPrefixes
                            .Select(x=>x.TrimEnd('/').Split('/').Last())
                            .Select(ushort.Parse)
                            .OrderByDescending(x => x).First() + 1
                    };
                return nextVersion.ToString("00000");
            }
            catch (AmazonS3Exception e)
            {
                throw new InvalidOperationException(e.Message, e);
            }
        }

        public string CreatePageCommentPrefix(string scoreName, string version, DateTimeOffset dateTime, string pageNo)
            => $"{scoreName}/versions/{version}/{dateTime:yyyyMMddHHmmssfff}/comments/{pageNo}/";

        public string CreateVersionFileKey(string scoreName, string version, DateTimeOffset dateTime)
            => $"{scoreName}/versions/{version}/{dateTime:yyyyMMddHHmmssfff}/version.json";
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

        public static string CreateKey() => DateTimeOffset.Now.UtcDateTime.ToString("yyyyMMddHHmmssfff");
    }

    public class ScoreContentMeta
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }

        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "version_file_keys")]
        public Dictionary<string,string> VersionFileKeys { get; set; } = new Dictionary<string, string>();

        public async Task<ScoreContentMeta> DeepCopyAsync()
        {
            using var mem = new MemoryStream();
            var options = new JsonSerializerOptions();
            await JsonSerializer.SerializeAsync(mem, this, options);

            mem.Position = 0;

            return await JsonSerializer.DeserializeAsync<ScoreContentMeta>(mem, options);
        }
    }

    public class ScoreVersionMeta
    {
        [JsonPropertyName(name: "version")]
        public int Version { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "pages")]
        public Dictionary<string,ScoreVersionPageMeta> Pages { get; set; } = new Dictionary<string, ScoreVersionPageMeta>();
    }

    public class ScoreVersionPageMeta
    {
        [JsonPropertyName(name: "no")]
        public string No { get; set; }

        [JsonPropertyName(name: "image_file_key")]
        public string ImageFileKey { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "comment_prefix")]
        public string CommentPrefix { get; set; }
        
        [JsonPropertyName(name: "overlay_svg_key")]
        public string OverlaySvgKey { get; set; }
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
                VersionFileKeys = current.VersionFileKeys,
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

        public async Task<Stream> ConvertToUtf(ScoreVersionMeta versionMeta)
        {
            var memStream = new MemoryStream();
            var option = new JsonSerializerOptions() { };
            await JsonSerializer.SerializeAsync(memStream, versionMeta, option);
            memStream.Position = 0;
            return memStream;
        }

        public async Task<ScoreMeta> ConvertToScoreMeta(Stream stream)
        {
            var option = new JsonSerializerOptions() { };
            return await JsonSerializer.DeserializeAsync<ScoreMeta>(stream, option);
        }

        public async Task<ScoreVersionMeta> ConvertToScoreVersionMeta(Stream stream)
        {
            var option = new JsonSerializerOptions() { };
            return await JsonSerializer.DeserializeAsync<ScoreVersionMeta>(stream, option);
        }

        public ScoreVersion ConvertTo(ScoreVersionMeta meta, MinioUrlConvertor urlConvertor)
        {
            var version = new ScoreVersion
            {
                Version = meta.Version,
            };

            version.Pages = meta.Pages.Select(x => new ScoreVersionPage()
            {
                No = double.Parse(x.Value.No),
                Url = urlConvertor.CreateUri(x.Value.ImageFileKey)
            }).ToArray();

            return version;
        }
    }

    public class MinioUrlConvertor
    {
        public string ServiceUrl { get; }
        public string BucketName { get; }

        public MinioUrlConvertor(string serviceUrl, string bucketName)
        {
            ServiceUrl = serviceUrl;
            BucketName = bucketName;
        }

        public Uri CreateUri(string key) => new Uri($"{ServiceUrl}/{BucketName}/{key}");

    }
}
