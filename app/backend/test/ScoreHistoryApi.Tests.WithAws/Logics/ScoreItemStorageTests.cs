using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItems;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Logics.Scores;
using Xunit;

namespace ScoreHistoryApi.Tests.WithAws.Logics
{
    public class ScoreItemStorageTests
    {

        public const string ScoreTableName = "ura-kata-score-history";

        public string RegionSystemName { get; set; }
        public string BucketName { get; set; }
        public IConfiguration Configuration { get; }

        public ScoreItemStorageTests()
        {
            var a = Path.GetDirectoryName(typeof(ScoreItemStorageTests).Assembly.Location);
            Console.WriteLine(a);
            var b = Directory.GetCurrentDirectory();
            Console.WriteLine(b);
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(typeof(ScoreItemStorageTests).Assembly.Location))
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            RegionSystemName = Configuration["URA_KATA:SCORE_HISTORY_TEST_AWS:REGION"];
            if (string.IsNullOrWhiteSpace(RegionSystemName))
                throw new InvalidOperationException($"'URA_KATA__SCORE_HISTORY_TEST_AWS__REGION' is not found.");
            BucketName = Configuration["URA_KATA:SCORE_HISTORY_TEST_AWS:BUCKET_NAME"];
            if (string.IsNullOrWhiteSpace(BucketName))
                throw new InvalidOperationException($"'URA_KATA__SCORE_HISTORY_TEST_AWS__BUCKET_NAME' is not found.");
        }

        [Fact]
        public async Task SetAccessControlPolicyAsyncTest()
        {
            var factory = new S3ClientFactory()
                .SetRegionSystemName(RegionSystemName);
            var dynamoDbClientFactory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var bucketName = BucketName;
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    [EnvironmentNames.ScoreDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreLargeDataDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemRelationDynamoDbTableName] = ScoreTableName,
                    [EnvironmentNames.ScoreItemS3Bucket] = BucketName,
                    [EnvironmentNames.ScoreDataSnapshotS3Bucket] = BucketName,
                })
                .Build();
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton(dynamoDbClientFactory.Create());
            serviceCollection.AddSingleton(factory.Create());
            serviceCollection.AddSingleton<ScoreQuota>();
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddScoped<Initializer>();
            serviceCollection.AddScoped<ScoreItemAdder>();
            serviceCollection.AddScoped<ScoreItemDeleter>();
            serviceCollection.AddScoped<ScoreItemInfoGetter>();
            serviceCollection.AddScoped<ScoreAccessSetter>();

            await using var provider = serviceCollection.BuildServiceProvider();

            var initializer = provider.GetRequiredService<Initializer>();
            var creator = provider.GetRequiredService<ScoreItemAdder>();
            var deleter = provider.GetRequiredService<ScoreItemDeleter>();
            var infoGetter = provider.GetRequiredService<ScoreItemInfoGetter>();
            var accessSetter = provider.GetRequiredService<ScoreAccessSetter>();

            var ownerId = Guid.Parse("4935be9f-8b08-4de9-a615-96ec04c2e4c5");
            var scoreIds = new[]
            {
                Guid.Parse("b6538dd3-2e2d-47bf-8ad3-1cb23b982309"),
                Guid.Parse("2ee917b6-94fa-469f-a977-14f57d9a7b4e"),
            };
            var itemIds = new[]
            {
                new Guid("6cf18791-0058-426b-b5fb-8f553079a6f5"),
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
                    await creator.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Private);
                }
            }

            await accessSetter.SetScoreItemAccessControlPolicyAsync(ownerId, scoreIds[0], ScoreObjectAccessControls.Public);
        }
    }
}
