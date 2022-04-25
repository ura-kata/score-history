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
    public class ScoreItemAdderTests
    {
        public const string ScoreTableName = "ura-kata-score-history";
        public const string ScoreBucket = "ura-kata-score-history-bucket";

        [Fact]
        public async Task CreateAsyncTest()
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

            var ownerId = Guid.Parse("5a56be69-af15-41a1-a879-08b6efd40eef");
            var scoreId = Guid.Parse("79867694-f52d-406c-b519-9091153cf5d3");
            var itemId = Guid.Parse("8228a9dd-d752-43f1-be84-31fce6b088d9");
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
                await deleter.DeleteAsync(ownerId, scoreId, itemId);
            }
            catch
            {
                // 握りつぶす
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
            await creator.CreateAsync(itemData);
        }

        [Fact]
        public async Task SaveObjectAsyncTest()
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
            var itemId = Guid.Parse("cc42d1b1-c6b0-4895-ba74-de6e89d853a1");

            var imageRelativeResourceName = "Resources.pexels-cottonbro-4709821.jpg";
            await using var imageStream = ResourceUtils.CreateResourceStream(imageRelativeResourceName);

            var data = new byte[imageStream.Length];
            await imageStream.ReadAsync(data, 0, data.Length);

            await creator.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);
        }
    }
}
