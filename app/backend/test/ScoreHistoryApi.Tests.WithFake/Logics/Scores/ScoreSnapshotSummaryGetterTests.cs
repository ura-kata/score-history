using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ScoreSnapshotSummaryGetterTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";


        [Fact]
        public async Task GetSnapshotNamesAsyncTest()
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



            var ownerId = Guid.Parse("afd15615-99b6-46c1-92fe-3da242d57e9d");
            var scoreId = Guid.Parse("89405e01-67f1-42e6-8673-e932a4b20d26");

            var title = "test score";
            var description = "楽譜の説明(スナップショット削除)";

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

            var newAnnotations = new List<NewScoreAnnotation>()
            {
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
                new NewScoreAnnotation(){Content = Guid.NewGuid().ToString()},
            };

            try
            {
                await annotationAdder.AddAnnotationsAsync(ownerId, scoreId, newAnnotations);
            }
            catch (Exception)
            {
                // 握りつぶす
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

            var snapshotNames = new[]
            {
                (id: new Guid("5542709f-2810-40c1-a3c4-fbcd2217fb65"), name: "スナップショット名1(Get)"),
                (id: new Guid("a2821d08-3e25-405f-8b7e-1b6543b86e02"), name: "スナップショット名2(Get)"),
                (id: new Guid("d191a5bc-ffca-4ab7-978e-4bf8236b4bdc"), name: "スナップショット名3(Get)"),
                (id: new Guid("e5a56aff-1449-48e0-acd1-89e8506bbb6b"), name: "スナップショット名4(Get)"),
            }.OrderBy(x => x).ToArray();

            try
            {
                foreach (var snapshotName in snapshotNames)
                {
                    await snapshotCreator.CreateSnapshotAsync(ownerId, scoreId, snapshotName.id, snapshotName.name);
                }
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            var actual = await snapshotSummaryGetter.GetSnapshotSummariesAsync(ownerId, scoreId);

            Assert.Equal(
                snapshotNames.OrderBy(x => x.id).Select(x=>(x.id,x.name)).ToArray(),
                actual.OrderBy(x => x.Id).Select(x=>(x.Id,x.Name)).ToArray()
                );
        }

    }
}
