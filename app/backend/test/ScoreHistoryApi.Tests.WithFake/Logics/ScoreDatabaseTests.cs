using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Models.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics
{
    public class ScoreDatabaseTests
    {
        [Fact]
        public async Task CreateAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history";
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");
            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var title = "test score";
            var description = "楽譜の説明";

            await target.CreateAsync(ownerId, scoreId, title, description);
        }

        [Fact]
        public async Task UpdateTitleAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history";
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newTitle = "new test score";
            await target.UpdateTitleAsync(ownerId, scoreId, newTitle);
        }

        [Fact]
        public async Task UpdateDescriptionAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history";
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newDescription = "新しい楽譜の説明";
            await target.UpdateDescriptionAsync(ownerId, scoreId, newDescription);
        }

        [Fact]
        public async Task AddPagesAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history";
            var target = new ScoreDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await target.CreateAsync(ownerId, scoreId, title, description);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var newPages = new List<NewScorePage>()
            {
                new NewScorePage()
                {
                    Page = "1",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "2",
                    ItemId = Guid.NewGuid(),
                }
            };
            await target.AddPagesAsync(ownerId, scoreId, newPages);
        }
    }
}
