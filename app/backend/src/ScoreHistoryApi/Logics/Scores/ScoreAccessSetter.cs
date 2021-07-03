using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreAccessSetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreAccessSetter(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IScoreQuota quota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _quota = quota;
            _configuration = configuration;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;


            var scoreItemS3Bucket = configuration[EnvironmentNames.ScoreItemS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreItemS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemS3Bucket}' is not found.");
            ScoreItemS3Bucket = scoreItemS3Bucket;

            var scoreDataSnapshotS3Bucket = configuration[EnvironmentNames.ScoreDataSnapshotS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreDataSnapshotS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreDataSnapshotS3Bucket}' is not found.");
            ScoreDataSnapshotS3Bucket = scoreDataSnapshotS3Bucket;
        }

        public string ScoreItemS3Bucket { get; set; }

        public string ScoreDataSnapshotS3Bucket { get; set; }

        public string ScoreTableName { get; set; }

        public async Task SetAccessAsync(Guid ownerId, Guid scoreId, PatchScoreAccess access)
        {
            await SetAccessAsync(ownerId, scoreId, access.Access);
            var accessControl = access.Access == ScoreAccesses.Public
                ? ScoreObjectAccessControls.Public
                : ScoreObjectAccessControls.Private;
            await SetScoreItemAccessControlPolicyAsync(ownerId, scoreId, accessControl);
            await SetSnapshotAccessControlPolicyAsync(ownerId, scoreId, accessControl);
        }


        public async Task SetSnapshotAccessControlPolicyAsync(Guid ownerId, Guid scoreId, ScoreObjectAccessControls accessControl)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}/{ScoreSnapshotStorageConstant.SnapshotFolderName}";

            var objectKeyList = new List<string>();
            string continuationToken = default;

            do
            {
                var listRequest = new ListObjectsV2Request()
                {
                    BucketName = ScoreDataSnapshotS3Bucket,
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
                    BucketName = ScoreDataSnapshotS3Bucket,
                    CannedACL = acl,
                    Key = key,
                };
                await _s3Client.PutACLAsync(request);
            }
        }



        public async Task SetScoreItemAccessControlPolicyAsync(Guid ownerId, Guid scoreId,
            ScoreObjectAccessControls accessControl)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}/{ScoreItemStorageConstant.FolderName}";

            var objectKeyList = new List<string>();
            string continuationToken = default;

            do
            {
                var listRequest = new ListObjectsV2Request()
                {
                    BucketName = ScoreItemS3Bucket,
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
                    BucketName = ScoreItemS3Bucket,
                    CannedACL = acl,
                    Key = key,
                };
                await _s3Client.PutACLAsync(request);
            }

        }


        public async Task SetAccessAsync(Guid ownerId, Guid scoreId, ScoreAccesses access)
        {
            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId, access, now);

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid scoreId,
                ScoreAccesses access,
                DateTimeOffset now
                )
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);
                var accessText = ScoreDatabaseUtils.ConvertFromScoreAccess(access);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.SortKey] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#score"] = DynamoDbScorePropertyNames.SortKey,
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#access"] = DynamoDbScorePropertyNames.Access,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":access"] = new AttributeValue(accessText),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "attribute_exists(#score)",
                    UpdateExpression = "SET #updateAt = :updateAt, #access = :access",
                    TableName = tableName,
                };
                try
                {
                    await client.UpdateItemAsync(request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }
    }
}
