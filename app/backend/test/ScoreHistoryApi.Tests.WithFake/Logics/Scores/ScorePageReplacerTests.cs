using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Models.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics.Scores
{
    public class ScorePageReplacerTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task ReplacePagesAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    [EnvironmentNames.ScoreDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreLargeDataDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemRelationDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemS3Bucket] = ScoreBucket,
                    [EnvironmentNames.ScoreDataSnapshotS3Bucket] = ScoreBucket,
                })
                .Build();
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(factory.Create());
            serviceCollection.AddSingleton<ScoreQuota>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddScoped<Initializer>();
            serviceCollection.AddScoped<ScoreDeleter>();
            serviceCollection.AddScoped<ScoreDetailGetter>();
            serviceCollection.AddScoped<ScoreCreator>();
            serviceCollection.AddScoped<ScorePageAdder>();
            serviceCollection.AddScoped<ScorePageRemover>();
            serviceCollection.AddScoped<ScorePageReplacer>();

            await using var provider = serviceCollection.BuildServiceProvider();


            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreCreator>();
            var deleter = provider.GetRequiredService<ScoreDeleter>();
            var getter = provider.GetRequiredService<ScoreDetailGetter>();
            var pageAdder = provider.GetRequiredService<ScorePageAdder>();
            var pageRemover = provider.GetRequiredService<ScorePageRemover>();
            var pageReplacer = provider.GetRequiredService<ScorePageReplacer>();

            var ownerId = Guid.Parse("f9aa6c43-aab0-485e-a5a7-f6dbf592b0a4");
            var scoreId = Guid.Parse("9db807b1-42fe-4e1a-b85c-61905b176616");

            var title = "test score";
            var description = "楽譜の説明";

            try
            {
                await initializer.InitializeScoreAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }
            try
            {
                await creator.CreateAsync(ownerId, scoreId, title, description);
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
                },
                new NewScorePage()
                {
                    Page = "3",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "4",
                    ItemId = Guid.NewGuid(),
                },
                new NewScorePage()
                {
                    Page = "5",
                    ItemId = Guid.NewGuid(),
                }
            };

            try
            {
                await pageAdder.AddPagesAsync(ownerId, scoreId, newPages);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var pages = new List<PatchScorePage>()
            {
                new PatchScorePage()
                {
                    TargetPageId = 1,
                    Page = "replaced " + Guid.NewGuid(),
                    ItemId = Guid.NewGuid()
                },
                new PatchScorePage()
                {
                    TargetPageId = 3,
                    Page = "replaced " + Guid.NewGuid(),
                    ItemId = Guid.NewGuid()
                },
            };
            await pageReplacer.ReplacePagesAsync(ownerId, scoreId, pages);

        }
    }
}
