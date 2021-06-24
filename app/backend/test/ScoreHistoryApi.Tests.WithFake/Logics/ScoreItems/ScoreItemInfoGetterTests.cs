using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Logics.ScoreItems;
using ScoreHistoryApi.Logics.Scores;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithFake.Logics.ScoreItems
{
    public class ScoreItemInfoGetterTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        private readonly ITestOutputHelper _outputHelper;

        public ScoreItemInfoGetterTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private async Task Initialize(Guid ownerId, (Guid scoreId, Guid[] itemIds)[] ids, ServiceProvider provider)
        {
            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreItemAdder>();
            var deleter = provider.GetRequiredService<ScoreItemDeleter>();
            var infoGetter = provider.GetRequiredService<ScoreItemInfoGetter>();

            try
            {
                var sw = Stopwatch.StartNew();
                await initializer.InitializeScoreItemAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            try
            {
                var sw = Stopwatch.StartNew();
                await deleter.DeleteOwnerItemsAsync(ownerId);
            }
            catch
            {
                // エラーは握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1;


            try
            {
                var sw = Stopwatch.StartNew();
                int i = 0;
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

                        if ((++i) % 200 == 0)
                        {
                            _outputHelper.WriteLine($"{i} / 2000");
                        }
                    }
                }
            }
            catch
            {
                // エラーは握りつぶす
            }
        }

        [Fact]
        public async Task GetOwnerItemsInfoAsyncTest()
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

            var ownerId = Guid.Parse("872c3d39-2e2e-4d70-9d6f-aaf2b20bd990");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("732e70e9-ec06-48da-a303-afd9060d5062"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
                (Guid.Parse("6b4db161-bd4d-45d9-9c1f-e6287beb18e5"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
            };

            await Initialize(ownerId, ids, provider);

            var sw = Stopwatch.StartNew();

            var data = await infoGetter.GetOwnerItemsInfoAsync(ownerId);


            Assert.Equal(2000, data.ItemInfos.Count);

            var expectedIds = ids
                .SelectMany(x => x.itemIds.Select(y => (s:x.scoreId,i: y)))
                .OrderBy(x => x)
                .ToArray();

            var actualIds = data.ItemInfos
                .Select(x => (s: x.ScoreId, i: x.ItemId))
                .OrderBy(x=>x)
                .ToArray();

            Assert.Equal(expectedIds, actualIds);
        }

        [Fact]
        public async Task GetUserItemsInfoAsyncTest()
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

            var ownerId = Guid.Parse("cbf6f217-ca22-48ba-81f1-9b9abfa0dfbb");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("0f5c6a0a-7f17-4531-bdb1-980b6ba11858"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
                (Guid.Parse("fff568e2-ceac-4550-b63d-a8dee0962eb3"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
            };

            await Initialize(ownerId, ids, provider);

            var sw = Stopwatch.StartNew();

            var data = await infoGetter.GetUserItemsInfoAsync(ownerId);


            Assert.Equal(2000, data.ItemInfos.Count);

            var expectedIds = ids
                .SelectMany(x => x.itemIds.Select(y => (s:x.scoreId,i: y)))
                .OrderBy(x => x)
                .ToArray();

            var actualIds = data.ItemInfos
                .Select(x => (s: x.ScoreId, i: x.ItemId))
                .OrderBy(x=>x)
                .ToArray();

            Assert.Equal(expectedIds, actualIds);
        }


        [Fact]
        public async Task GetItemsAsyncTest()
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

            var ownerId = Guid.Parse("6f76e99b-6835-4067-b4ff-22d3eb1d1c33");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("a4442515-24b2-490d-bb5b-7446e1be1e0b"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
                (Guid.Parse("9b318593-66de-45c4-a9b3-f1f5a05bb52f"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
            };

            try
            {
                await initializer.InitializeScoreItemAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            try
            {
                await deleter.DeleteOwnerItemsAsync(ownerId);
            }
            catch
            {
                // エラーは握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1;


            try
            {
                var sw = Stopwatch.StartNew();
                int i = 0;
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

                        if ((++i) % 200 == 0)
                        {
                            _outputHelper.WriteLine($"{i} / 2000");
                        }
                    }
                }
            }
            catch
            {
                // エラーは握りつぶす
            }

            var data = await infoGetter.GetItemsAsync(ownerId);

            Assert.Equal(2000, data.Length);

            var expectedIds = ids
                .SelectMany(x => x.itemIds.Select(y => (s:x.scoreId,i: y)))
                .OrderBy(x => x)
                .ToArray();

            var actualIds = data
                .Select(x => (s: x.ScoreId, i: x.ItemId))
                .OrderBy(x=>x)
                .ToArray();

            Assert.Equal(expectedIds, actualIds);
        }
    }
}
