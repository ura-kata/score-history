using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeManagerApi.Services.Models;

namespace PracticeManagerApi.Mock.Controllers.v1
{
    [Route("api/v1/score_v2")]
    public class ScoreV2Controller : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private ILogger _logger;

        public ScoreV2Controller(IConfiguration configuration, ILogger<ScoreV2Controller> logger)
        {
            _configuration = configuration;
            this._logger = logger;
        }

        [HttpGet]
        [Route("{owner}")]
        public async IAsyncEnumerable<ScoreWithOwner> GetScoreNameListWithOwner(
            [FromRoute(Name = "owner")]
            [MaxLength(128, ErrorMessage = "{0} は 128 文字以内です")]
            [MinLength(1, ErrorMessage = "{0} は 1 文字以上です")]
            [RegularExpression(@"^[a-zA-Z0-9\-_]+$",ErrorMessage = "{0} は 半角英数字 , - , _ が使用できます", MatchTimeoutInMilliseconds = 1000)]
            string owner,
            [FromQuery(Name = "q")]
            string q)
        {
            var scoresDir = _configuration["ScoresDirectory"];
            var ownerDirectoryPath = Path.Join(scoresDir, owner);


            if (false ==Directory.Exists(ownerDirectoryPath))
            {
                throw new InvalidOperationException($"'{owner}' は存在しません");
            }

            var scoreNames = Directory.GetDirectories(ownerDirectoryPath)
                .Select(path=>path.TrimEnd('/', '\\').Split('/', '\\').Last())
                .Where(name=>string.IsNullOrWhiteSpace(q) || name.Contains(q))
                .ToArray();

            var scoreDetailFileName = _configuration["ScoreDetailFileName"];

            foreach (var scoreName in scoreNames)
            {
                var filePath = Path.Join(ownerDirectoryPath, scoreName, scoreDetailFileName);
                NewScoreWithOwner detail = null;
                if(System.IO.File.Exists(filePath))
                {
                    await using var stream = System.IO.File.OpenRead(filePath);
                    JsonSerializerOptions options = new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    detail = await JsonSerializer.DeserializeAsync<NewScoreWithOwner>(stream, options);
                }

                yield return new ScoreWithOwner()
                {
                    Name = scoreName,
                    Owner = owner,
                    Title = detail?.Title,
                    Description = detail?.Description,
                };
            }
        }

        [HttpPost]
        [Route("{owner}/{score_name}")]
        public async Task<IActionResult> CreateScoreWithOwnerAsync(
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
            if (string.IsNullOrWhiteSpace(body.Title) || !(1 <= body.Title.Length && body.Title.Length <= 128))
            {
                return BadRequest();
            }
            if (body.Description != null && !(body.Description.Length <= 1024))
            {
                return BadRequest();
            }

            var scoresDir = _configuration["ScoresDirectory"];
            var scoreDirectoryPath = Path.Join(scoresDir, owner, scoreName);


            if ( Directory.Exists(scoreDirectoryPath))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' はすでに存在します");
            }

            try
            {
                Directory.CreateDirectory(scoreDirectoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new InvalidOperationException("作成に失敗しました");
            }

            var scoreDetailFileName = _configuration["ScoreDetailFileName"];
            var filePath = Path.Join(scoreDirectoryPath, scoreDetailFileName);
            await using Stream stream = System.IO.File.OpenWrite(filePath);
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            await JsonSerializer.SerializeAsync(stream, body, options);
            stream.Close();

            return Ok();
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
            var scoresDir = _configuration["ScoresDirectory"];
            var scoreDirectoryPath = Path.Join(scoresDir, owner, scoreName);


            if (false == Directory.Exists(scoreDirectoryPath))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' は存在しません");
            }

            try
            {
                Directory.Delete(scoreDirectoryPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new InvalidOperationException("削除に失敗しました");
            }

            return Ok();
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
            if (body.Title != null && !(1 <= body.Title.Length && body.Title.Length <= 128))
            {
                return BadRequest();
            }
            if (body.Description!= null && !(body.Description.Length <= 1024))
            {
                return BadRequest();
            }

            var scoresDir = _configuration["ScoresDirectory"];
            var scoreDirectoryPath = Path.Join(scoresDir, owner, scoreName);


            if (false == Directory.Exists(scoreDirectoryPath))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' は存在しません");
            }
            
            var scoreDetailFileName = _configuration["ScoreDetailFileName"];
            var filePath = Path.Join(scoreDirectoryPath, scoreDetailFileName);

            if (false == System.IO.File.Exists(filePath))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' の詳細が存在しません");
            }
            NewScoreWithOwner score;
            {
                await using Stream stream = System.IO.File.OpenRead(filePath);
                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                score = await JsonSerializer.DeserializeAsync<NewScoreWithOwner>(stream, options);
            }

            System.IO.File.Delete(filePath);

            {
                if (body.Title != null)
                {
                    score.Title = body.Title;
                }
                if (body.Description!= null)
                {
                    score.Description = body.Description;
                }

                await using Stream stream = System.IO.File.OpenWrite(filePath);
                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                await JsonSerializer.SerializeAsync(stream, score, options);
                stream.Close();
            }

            return Ok();
        }

        [HttpGet]
        [Route("{owner}/{score_name}")]
        public IActionResult GetScoreWithOwner(
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
            var scoresDir = _configuration["ScoresDirectory"];
            var scoreDirectoryPath = Path.Join(scoresDir, owner, scoreName);


            if (false == Directory.Exists(scoreDirectoryPath))
            {
                throw new InvalidOperationException($"'{owner}/{scoreName}' は存在しません");
            }

            try
            {
                Directory.Delete(scoreDirectoryPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new InvalidOperationException("削除に失敗しました");
            }

            return Ok();
        }
    }
}
