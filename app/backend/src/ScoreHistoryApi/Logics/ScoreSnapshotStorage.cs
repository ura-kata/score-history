using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics
{
    public static class ScoreSnapshotStorageUtils
    {
        public static byte[] ConvertToJson(ScoreSnapshot snapshot)
        {
            var option = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = false,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            return JsonSerializer.SerializeToUtf8Bytes(snapshot, option);
        }

        public static string CreateSnapshotKey(Guid ownerId, Guid scoreId, Guid snapshotId) =>
            $"{ownerId:D}/{scoreId:D}/{snapshotId:D}.json";
    }

    /// <summary>
    /// スナップショットのストレージ
    /// </summary>
    public class ScoreSnapshotStorage : IScoreSnapshotStorage
    {
        public string BucketName { get; } = "ura-kata-score-snapshot-bucket";
        private readonly IAmazonS3 _s3Client;

        public ScoreSnapshotStorage(IAmazonS3 s3Client, IConfiguration configuration)
        {
            var bucketName = configuration[EnvironmentNames.ScoreDataSnapshotS3Bucket];
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDataSnapshotS3Bucket}' is not found.");
            }

            BucketName = bucketName;
            _s3Client = s3Client;
        }

        public ScoreSnapshotStorage(IAmazonS3 s3Client, string bucketName)
        {
            BucketName = bucketName;
            _s3Client = s3Client;
        }

        public async Task CreateAsync(Guid ownerId, Guid scoreId, ScoreSnapshot snapshot,
            ScoreObjectAccessControls accessControl)
        {
            var key = ScoreSnapshotStorageUtils.CreateSnapshotKey(ownerId, scoreId, snapshot.id);

            var json = ScoreSnapshotStorageUtils.ConvertToJson(snapshot);

            await using var jsonStream = new MemoryStream(json);

            var acl = accessControl switch
            {
                ScoreObjectAccessControls.Private => S3CannedACL.Private,
                ScoreObjectAccessControls.Public => S3CannedACL.PublicRead,
                _ => throw new NotSupportedException(),
            };

            var request = new PutObjectRequest()
            {
                BucketName = BucketName,
                Key = key,
                CannedACL = acl,
                InputStream = jsonStream,
            };
            await _s3Client.PutObjectAsync(request);
        }

        public async Task DeleteAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            var key = ScoreSnapshotStorageUtils.CreateSnapshotKey(ownerId, scoreId, snapshotId);

            var request = new DeleteObjectRequest()
            {
                BucketName = BucketName,
                Key = key,
            };
            var response = await _s3Client.DeleteObjectAsync(request);
        }

        public async Task DeleteAllAsync(Guid ownerId, Guid scoreId)
        {
            var objectKeyList = new List<string>();
            string continuationToken = default;

            var prefix = $"{ownerId:D}/{scoreId:D}";

            do
            {
                var listRequest = new ListObjectsV2Request()
                {
                    BucketName = BucketName,
                    Prefix = prefix,
                    ContinuationToken = string.IsNullOrWhiteSpace(continuationToken) ? null : continuationToken,
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                objectKeyList.AddRange(listResponse.S3Objects.Select(x => x.Key));

                continuationToken = listResponse.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));

            var request = new DeleteObjectsRequest()
            {
                BucketName = BucketName,
                Objects = objectKeyList.Select(x=>new KeyVersion()
                {
                    Key = x
                }).ToList(),
            };
            await _s3Client.DeleteObjectsAsync(request);
        }

        public async Task SetAccessControlPolicyAsync(Guid ownerId, Guid scoreId, ScoreObjectAccessControls accessControl)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}";

            var objectKeyList = new List<string>();
            string continuationToken = default;

            do
            {
                var listRequest = new ListObjectsV2Request()
                {
                    BucketName = BucketName,
                    Prefix = prefix,
                    ContinuationToken = string.IsNullOrWhiteSpace(continuationToken) ? null : continuationToken,
                };
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                objectKeyList.AddRange(listResponse.S3Objects.Select(x => x.Key));

                continuationToken = listResponse.NextContinuationToken;

            } while (!string.IsNullOrEmpty(continuationToken));


            var acl = accessControl switch
            {
                ScoreObjectAccessControls.Private => S3CannedACL.Private,
                ScoreObjectAccessControls.Public => S3CannedACL.PublicRead,
                _ => throw new NotSupportedException(),
            };

            foreach (var key in objectKeyList)
            {
                var request = new PutACLRequest()
                {
                    BucketName = BucketName,
                    CannedACL = acl,
                    Key = key,
                };
                await _s3Client.PutACLAsync(request);
            }
        }
    }
}
