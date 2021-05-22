using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using Xunit;

namespace ScoreHistoryApi.Tests.WithAws.Logics
{
    public class ScoreItemStorageTests
    {
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
            var bucketName = BucketName;
            var target = new ScoreItemStorage(new ScoreQuota(), factory.Create(), bucketName);

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
                    await target.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Private);
                }
            }

            await target.SetAccessControlPolicyAsync(ownerId, scoreIds[0], ScoreObjectAccessControls.Public);
        }
    }
}
