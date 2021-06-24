using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Logics.ScoreItems;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Logics.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics.ScoreItems
{
    public class ScoreItemDeleterTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task DeleteAsyncTest()
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
            serviceCollection.AddScoped<ScoreItemAdder>();
            serviceCollection.AddScoped<ScoreItemDeleter>();
            serviceCollection.AddScoped<ScoreItemInfoGetter>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreItemAdder>();
            var deleter = provider.GetRequiredService<ScoreItemDeleter>();
            var infoGetter = provider.GetRequiredService<ScoreItemInfoGetter>();

            var ownerId = Guid.Parse("a585178e-a477-4fb8-8e2a-c385a45c0d08");
            var scoreId = Guid.Parse("2533ffdd-9624-4c80-979f-95561edf5ed1");
            var itemId = Guid.Parse("67ff2dbd-c6d4-41c1-a9e5-857b56a09361");
            try
            {
                await initializer.InitializeScoreItemAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1024;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1024;

            var itemData = new ScoreItemDatabaseItemDataImage()
            {
                OwnerId = ownerId,
                ScoreId = scoreId,
                ItemId = itemId,
                ObjName = objName,
                OrgName = orgName,
                Size = size,
                Thumbnail = new ScoreItemDatabaseItemDataImageThumbnail()
                {
                    ObjName = thumbnailObjName,
                    Size = thumbnailSize,
                }
            };

            try
            {
                await creator.CreateAsync(itemData);
            }
            catch
            {
                // エラーは握りつぶす
            }

            await deleter.DeleteAsync(ownerId, scoreId, itemId);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await infoGetter.GetItemAsync(ownerId, scoreId, itemId));
        }


        [Fact]
        public async Task DeleteOwnerItemsAsyncTest()
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
            serviceCollection.AddScoped<ScoreItemAdder>();
            serviceCollection.AddScoped<ScoreItemDeleter>();
            serviceCollection.AddScoped<ScoreItemInfoGetter>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreItemAdder>();
            var deleter = provider.GetRequiredService<ScoreItemDeleter>();
            var infoGetter = provider.GetRequiredService<ScoreItemInfoGetter>();

            var ownerId = Guid.Parse("39b6bf0e-8c39-47ce-bae5-49c557f8d2fa");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("361aaffb-6151-4bd4-a0a8-06af87a04565"),
                    new[]
                    {
                        Guid.Parse("b45282b1-88e0-4079-beaf-583f58879bd9"),
                        Guid.Parse("d41a3cd5-38a4-45bb-813a-53be5867b4ca"),
                    }),
                (Guid.Parse("a8a5e141-633b-4481-ba50-d14fdde969a7"),
                    new[]
                    {
                        Guid.Parse("daaea941-c626-4d03-9a2a-adbba45a9437"),
                        Guid.Parse("a75f6884-ae65-4eb0-91c1-5a65239319c6"),
                    }),
            };

            try
            {
                await initializer.InitializeScoreItemAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1024;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1024;


            try
            {
                foreach (var (scoreId, itemIds) in ids)
                {
                    foreach (var itemId in itemIds)
                    {
                        var itemData = new ScoreItemDatabaseItemDataImage()
                        {
                            OwnerId = ownerId,
                            ScoreId = scoreId,
                            ItemId = itemId,
                            ObjName = objName,
                            OrgName = orgName,
                            Size = size,
                            Thumbnail = new ScoreItemDatabaseItemDataImageThumbnail()
                            {
                                ObjName = thumbnailObjName,
                                Size = thumbnailSize,
                            }
                        };

                        await creator.CreateAsync(itemData);
                    }
                }
            }
            catch
            {
                // エラーは握りつぶす
            }

            await deleter.DeleteOwnerItemsAsync(ownerId);

        }

        [Fact]
        public async Task DeleteObjectAsyncTest()
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
            serviceCollection.AddScoped<ScoreItemAdder>();
            serviceCollection.AddScoped<ScoreItemDeleter>();
            serviceCollection.AddScoped<ScoreItemInfoGetter>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreItemAdder>();
            var deleter = provider.GetRequiredService<ScoreItemDeleter>();
            var infoGetter = provider.GetRequiredService<ScoreItemInfoGetter>();

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");
            var itemId = Guid.Parse("b1d822ec-908e-493f-aa01-49488b21b4be");

            var imageRelativeResourceName = "Resources.pexels-cottonbro-4709821.jpg";
            await using var imageStream = ResourceUtils.CreateResourceStream(imageRelativeResourceName);

            var data = new byte[imageStream.Length];
            await imageStream.ReadAsync(data, 0, data.Length);

            await creator.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);

            await deleter.DeleteObjectAsync(ownerId, scoreId, itemId);
        }


        [Fact]
        public async Task DeleteAllScoreObjectAsyncTest()
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
            serviceCollection.AddScoped<ScoreItemAdder>();
            serviceCollection.AddScoped<ScoreItemDeleter>();
            serviceCollection.AddScoped<ScoreItemInfoGetter>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreItemAdder>();
            var deleter = provider.GetRequiredService<ScoreItemDeleter>();
            var infoGetter = provider.GetRequiredService<ScoreItemInfoGetter>();

            var ownerId = Guid.Parse("ba08aaea-bfe4-4ae2-baaa-bf0623a92f5b");
            var scoreId = Guid.Parse("b6538dd3-2e2d-47bf-8ad3-1cb23b982309");
            var itemIds = new Guid[]
            {
                new Guid("6cf18791-0058-426b-b5fb-8f553079a6f5"),
                new Guid("fbe9e7f1-a96e-4951-8d65-f10e8b32a5ed"),
                new Guid("4650dce1-1833-4b1c-855d-a48d09a50fe3"),
                new Guid("15779f73-5d4a-46c1-bafc-f5fdc57f7f54"),
                new Guid("1e69d895-5487-4de6-ae8e-e0f57cd86954"),
            };

            var imageRelativeResourceName = "Resources.pexels-cottonbro-4709821.jpg";
            await using var imageStream = ResourceUtils.CreateResourceStream(imageRelativeResourceName);

            var data = new byte[imageStream.Length];
            await imageStream.ReadAsync(data, 0, data.Length);

            foreach (var itemId in itemIds)
            {
                await creator.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);
            }

            await deleter.DeleteAllScoreObjectAsync(ownerId, scoreId);
        }


        [Fact]
        public async Task DeleteAllOwnerObjectAsyncTest()
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
            serviceCollection.AddScoped<ScoreItemAdder>();
            serviceCollection.AddScoped<ScoreItemDeleter>();
            serviceCollection.AddScoped<ScoreItemInfoGetter>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreItemAdder>();
            var deleter = provider.GetRequiredService<ScoreItemDeleter>();
            var infoGetter = provider.GetRequiredService<ScoreItemInfoGetter>();

            var ownerId = Guid.Parse("584066a2-54ea-4430-8057-671c6aa69f84");
            var scoreIds = new[]
            {
                Guid.Parse("b6538dd3-2e2d-47bf-8ad3-1cb23b982309"),
                Guid.Parse("2ee917b6-94fa-469f-a977-14f57d9a7b4e"),
            };
            var itemIds = new[]
            {
                new Guid("6cf18791-0058-426b-b5fb-8f553079a6f5"),
                new Guid("fbe9e7f1-a96e-4951-8d65-f10e8b32a5ed"),
                new Guid("4650dce1-1833-4b1c-855d-a48d09a50fe3"),
                new Guid("15779f73-5d4a-46c1-bafc-f5fdc57f7f54"),
                new Guid("1e69d895-5487-4de6-ae8e-e0f57cd86954"),
            };

            var imageRelativeResourceName = "Resources.pexels-cottonbro-4709821.jpg";
            await using var imageStream = ResourceUtils.CreateResourceStream(imageRelativeResourceName);

            var data = new byte[imageStream.Length];
            await imageStream.ReadAsync(data, 0, data.Length);

            foreach (var scoreId in scoreIds)
            {
                foreach (var itemId in itemIds)
                {
                    await creator.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);
                }
            }

            await deleter.DeleteAllOwnerObjectAsync(ownerId);
        }
    }
}
