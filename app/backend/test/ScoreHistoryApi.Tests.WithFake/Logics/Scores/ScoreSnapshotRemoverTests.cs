using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Logics.Scores;
using ScoreHistoryApi.Models.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics.Scores
{
    public class ScoreSnapshotRemoverTests
    {

        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task DeleteSnapshotAsyncTest()
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



            var ownerId = Guid.Parse("3656f0fe-0068-4019-acc3-db042f6684b3");
            var scoreId = Guid.Parse("aa917a9b-453e-4bc2-8381-b61404725d6a");
            var snapshotId = Guid.Parse("7a82b4e0-02aa-4b5e-b323-7f13f61302c7");

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
                await annotationAdder.AddAnnotationsInnerAsync(ownerId, scoreId, newAnnotations);
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

            string snapshotName = "snapshot name(delete)";

            try
            {
                await snapshotCreator.CreateSnapshotAsync(ownerId, scoreId, snapshotId, snapshotName);
            }
            catch (Exception)
            {
                // 握りつぶす
            }

            await snapshotRemover.DeleteSnapshotAsync(ownerId, scoreId, snapshotId);

        }

        [Fact]
        public async Task DeleteAsyncTest()
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

            var ownerId = Guid.Parse("31937be9-d1df-4076-b2b6-9cb5e2d94a17");
            var scoreId = Guid.Parse("8a9aaa16-2ca2-4b22-9cf5-21e7b681dbc9");
            var snapshotId = Guid.Parse("f30cd5f5-b807-4273-9299-f95d0baf85b9");

            var data = new ScoreSnapshotDetail()
            {
                Id = snapshotId,
                Name = "スナップショット名(delete)",
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

            try
            {
                await creator.CreateSnapshotItemAsync(ownerId, scoreId, data, ScoreObjectAccessControls.Public);
            }
            catch
            {
                // 握りつぶす
            }

            await deleter.DeleteSnapshotAsync(ownerId, scoreId, snapshotId);
        }


        [Fact]
        public async Task DeleteAllAsyncTest()
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
            serviceCollection.AddScoped<ScoreDeleter>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreSnapshotCreator>();
            var deleter = provider.GetRequiredService<ScoreDeleter>();

            var ownerId = Guid.Parse("e9ca7322-9dd6-4429-b1d8-d3c9244a68ed");
            var scoreId = Guid.Parse("eea7e8c1-15ec-4f69-a675-364f24099267");

            var snapshotIds = new[]
            {
                Guid.Parse("0ea2f185-8355-439c-a427-d5734c17f886"),
                Guid.Parse("18be42eb-de3c-4899-bdc3-7ed512ce07ae"),
                Guid.Parse("7569ad9a-1bb4-4c33-9cb8-11e5c680a404"),
            };

            var data = new ScoreSnapshotDetail()
            {
                Name = "スナップショット名(delete all)",
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

            try
            {
                foreach (var snapshotId in snapshotIds)
                {
                    data.Id = snapshotId;
                    await creator.CreateSnapshotItemAsync(ownerId, scoreId, data, ScoreObjectAccessControls.Public);
                }
            }
            catch
            {
                // 握りつぶす
            }

            await deleter.DeleteAllSnapshotAsync(ownerId, scoreId);
        }
    }
}
