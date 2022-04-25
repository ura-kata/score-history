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
    public class ScorePageAdderTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task AddPagesAsyncTest()
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
            serviceCollection.AddScoped<ScoreCreator>();
            serviceCollection.AddScoped<ScorePageAdder>();

            await using var provider = serviceCollection.BuildServiceProvider();


            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreCreator>();

            var target = provider.GetRequiredService<ScorePageAdder>();

            var ownerId = Guid.Parse("13eff3dd-beb9-4471-adc4-dc4540ee3445");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");

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
                }
            };
            await target.AddPagesAsync(ownerId, scoreId, newPages);
        }


    }
}
