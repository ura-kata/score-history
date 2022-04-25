using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSnapshotDetailGetter
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public ScoreSnapshotDetailGetter(IAmazonS3 s3Client,IConfiguration configuration)
        {
            _s3Client = s3Client;
            _configuration = configuration;


            var scoreDataSnapshotS3Bucket = configuration[EnvironmentNames.ScoreDataSnapshotS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreDataSnapshotS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreDataSnapshotS3Bucket}' is not found.");
            ScoreDataSnapshotS3Bucket = scoreDataSnapshotS3Bucket;
        }

        public string ScoreDataSnapshotS3Bucket { get; set; }

        public async Task<ScoreSnapshotDetail> GetScoreSummaries(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            return await GetAsync(ownerId, scoreId, snapshotId);
        }


        public async Task<ScoreSnapshotDetail> GetAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            var key = ScoreSnapshotStorageUtils.CreateSnapshotKey(ownerId, scoreId, snapshotId);

            var request = new GetObjectRequest()
            {
                BucketName = ScoreDataSnapshotS3Bucket,
                Key = key,
            };
            try
            {
                var response = await _s3Client.GetObjectAsync(request);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    byte[] buffer = new byte[response.ResponseStream.Length];
                    await response.ResponseStream.ReadAsync(buffer);

                    return ScoreSnapshotStorageUtils.MapFromJson(buffer);
                }
            }
            catch (Exception ex)
            {
                throw new NotFoundSnapshotException(ex);
            }
            throw new NotFoundSnapshotException("Not found snapshot.");
        }
    }
}
