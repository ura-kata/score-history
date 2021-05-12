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
    }
}
