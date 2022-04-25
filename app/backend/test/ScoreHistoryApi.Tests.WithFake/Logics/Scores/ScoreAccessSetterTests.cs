using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics.Scores
{
    public class ScoreAccessSetterTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task SetAccessAsyncTest()
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
            serviceCollection.AddScoped<ScoreAnnotationAdder>();
            serviceCollection.AddScoped<ScoreAnnotationRemover>();
            serviceCollection.AddScoped<ScoreAnnotationReplacer>();
            serviceCollection.AddScoped<ScoreSnapshotCreator>();
            serviceCollection.AddScoped<ScoreSnapshotRemover>();
            serviceCollection.AddScoped<ScoreSnapshotDetailGetter>();
            serviceCollection.AddScoped<ScoreSnapshotSummaryGetter>();
            serviceCollection.AddScoped<ScoreAccessSetter>();

            await using var provider = serviceCollection.BuildServiceProvider();


            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreCreator>();
            var deleter = provider.GetRequiredService<ScoreDeleter>();
            var getter = provider.GetRequiredService<ScoreDetailGetter>();
            var pageAdder = provider.GetRequiredService<ScorePageAdder>();
            var pageRemover = provider.GetRequiredService<ScorePageRemover>();
            var annotationAdder = provider.GetRequiredService<ScoreAnnotationAdder>();
            var annotationRemover = provider.GetRequiredService<ScoreAnnotationRemover>();
            var annotationReplacer = provider.GetRequiredService<ScoreAnnotationReplacer>();
            var snapshotCreator = provider.GetRequiredService<ScoreSnapshotCreator>();
            var snapshotRemover = provider.GetRequiredService<ScoreSnapshotRemover>();
            var snapshotDetailGetter = provider.GetRequiredService<ScoreSnapshotDetailGetter>();
            var snapshotSummaryGetter = provider.GetRequiredService<ScoreSnapshotSummaryGetter>();
            var accessSetter = provider.GetRequiredService<ScoreAccessSetter>();



            var ownerId = Guid.Parse("721be298-bf3f-40f1-9f75-0679c4d06147");
            var scoreId = Guid.Parse("6884c1c3-55ab-4c62-b515-a039d18b14e9");

            var title = "test score";
            var description = "楽譜の説明(set access)";

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

            await accessSetter.SetAccessAsync(ownerId, scoreId, ScoreAccesses.Public);
        }
    }
}
