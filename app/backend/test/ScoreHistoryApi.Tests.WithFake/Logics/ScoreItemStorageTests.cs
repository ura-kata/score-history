using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Runtime;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using Xunit;

namespace ScoreHistoryApi.Tests.WithFake.Logics
{
    public class ScoreItemStorageTests
    {
        [Fact]
        public async Task SaveObjectAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var factory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));
            var bucketName = "ura-kata-test-bucket";
            var target = new ScoreItemStorage(new ScoreQuota(), factory.Create(), bucketName);

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");
            var itemId = Guid.Parse("cc42d1b1-c6b0-4895-ba74-de6e89d853a1");

            var imageRelativeResourceName = "Resources.pexels-cottonbro-4709821.jpg";
            await using var imageStream = ResourceUtils.CreateResourceStream(imageRelativeResourceName);

            var data = new byte[imageStream.Length];
            await imageStream.ReadAsync(data, 0, data.Length);

            await target.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);
        }

        [Fact]
        public async Task DeleteObjectAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var factory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));
            var bucketName = "ura-kata-test-bucket";
            var target = new ScoreItemStorage(new ScoreQuota(), factory.Create(), bucketName);

            var ownerId = Guid.Parse("f2240c15-0f2d-41ce-941d-6b173bae94c0");
            var scoreId = Guid.Parse("0d9fb491-43ae-44a1-9056-55bb25b21187");
            var itemId = Guid.Parse("b1d822ec-908e-493f-aa01-49488b21b4be");

            var imageRelativeResourceName = "Resources.pexels-cottonbro-4709821.jpg";
            await using var imageStream = ResourceUtils.CreateResourceStream(imageRelativeResourceName);

            var data = new byte[imageStream.Length];
            await imageStream.ReadAsync(data, 0, data.Length);

            await target.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);

            await target.DeleteObjectAsync(ownerId, scoreId, itemId);
        }

        [Fact]
        public async Task DeleteAllScoreObjectAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var factory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));
            var bucketName = "ura-kata-test-bucket";
            var target = new ScoreItemStorage(new ScoreQuota(), factory.Create(), bucketName);

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
                await target.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);
            }

            await target.DeleteAllScoreObjectAsync(ownerId, scoreId);
        }

        [Fact]
        public async Task DeleteAllOwnerObjectAsyncTest()
        {
            var accessKey = "minio_test";
            var secretKey = "minio_test_pass";
            var factory = new S3ClientFactory()
                .SetEndpointUrl(new Uri("http://localhost:19000"))
                .SetCredentials(new BasicAWSCredentials(accessKey, secretKey));
            var bucketName = "ura-kata-test-bucket";
            var target = new ScoreItemStorage(new ScoreQuota(), factory.Create(), bucketName);

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
                    await target.SaveObjectAsync(ownerId, scoreId, itemId, data, ScoreObjectAccessControls.Public);
                }
            }

            await target.DeleteAllOwnerObjectAsync(ownerId);
        }
    }
}
