using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItems;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Models.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics.Scores
{
    public class ScoreSnapshotCreatorTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task CreateSnapshotAsyncTest()
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


            var ownerId = Guid.Parse("4f0d25c8-0b33-4c00-92dc-2e85c3ac58a5");
            var scoreId = Guid.Parse("fd32d482-477d-4cb4-ab78-88e86a073a31");
            var snapshotId = Guid.Parse("6d1d0a52-8371-4f78-b61b-785522d2577d");

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

            string snapshotName = "snapshot name";

            await snapshotCreator.CreateSnapshotAsync(ownerId, scoreId, snapshotId, snapshotName);

        }


        [Fact]
        public async Task CreateAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var s3ClientFactory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));

            var dynamoDbClientFactory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
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

            serviceCollection.AddSingleton(dynamoDbClientFactory.Create());
            serviceCollection.AddSingleton(s3ClientFactory.Create());
            serviceCollection.AddSingleton<ScoreQuota>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddScoped<Initializer>();
            serviceCollection.AddScoped<ScoreSnapshotCreator>();
            serviceCollection.AddScoped<ScoreSnapshotRemover>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreSnapshotCreator>();
            var deleter = provider.GetRequiredService<ScoreSnapshotRemover>();



            var ownerId = Guid.Parse("80b16bc7-5258-441f-9a2a-d6d95fc16c4a");
            var scoreId = Guid.Parse("fc4ac609-0914-4cd6-9caa-cada12c7b03d");
            var snapshotId = Guid.Parse("3dd43d78-ff83-46f5-8ed3-fddbc06ec943");

            var data = new ScoreSnapshotDetail()
            {
                Id = snapshotId,
                Name = "スナップショット名",
                Data = new ScoreData()
                {
                    Title = "楽譜",
                    DescriptionHash = "楽譜の説明",
                    Annotations = new []{
                        new ScoreAnnotation()
                        {
                            Id = 0,
                            ContentHash = "hash00",
                        },
                        new ScoreAnnotation()
                        {
                            Id = 1,
                            ContentHash = "hash01",
                        },
                    },
                    Pages = new []
                    {
                        new ScorePage()
                        {
                            Id = 0,
                            Page = "page1",
                            ItemId = new Guid("3b74ca20-0e47-49b4-941f-45176766ae7d"),
                        },
                        new ScorePage()
                        {
                            Id = 1,
                            Page = "page2",
                            ItemId = new Guid("e3c0a4a6-344d-4247-9932-070ae822186b"),
                        },
                    },
                },
                HashSet = new Dictionary<string, string>()
                {
                    ["hash00"] = "アノテーション1",
                    ["hash01"] = "アノテーション2",
                },
            };
            await creator.CreateSnapshotItemAsync(ownerId, scoreId, data, ScoreObjectAccessControls.Public);
        }
    }
}
