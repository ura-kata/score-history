using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics
{
    public static class ScoreSnapshotStorageConstant
    {
        public const string SnapshotFolderName = "snapshot";
    }
    public static class ScoreSnapshotStorageUtils
    {
        public static byte[] ConvertToJson(ScoreSnapshotDetail snapshotDetail)
        {

            var option = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                DictionaryKeyPolicy = default,
                IgnoreNullValues = false,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = default,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            return JsonSerializer.SerializeToUtf8Bytes(snapshotDetail, option);
        }

        public static ScoreSnapshotDetail MapFromJson(byte[] jsonData)
        {
            var option = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                DictionaryKeyPolicy = default,
                IgnoreNullValues = false,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = default,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            return JsonSerializer.Deserialize<ScoreSnapshotDetail>(jsonData, option);
        }

        public static string CreateSnapshotKey(Guid ownerId, Guid scoreId, Guid snapshotId) =>
            $"{ownerId:D}/{scoreId:D}/{ScoreSnapshotStorageConstant.SnapshotFolderName}/{snapshotId:D}.json";
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

    }
}
