using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.DynamoDb;
using ScoreHistoryApi.Logics.DynamoDb.PropertyNames;
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
        private readonly IScoreCommonLogic _commonLogic;

        public ScoreAccessSetter(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IScoreQuota quota, IConfiguration configuration, IScoreCommonLogic commonLogic)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _quota = quota;
            _configuration = configuration;
            _commonLogic = commonLogic;


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
            var now = _commonLogic.Now;
            var tableName = ScoreTableName;
            var client = _dynamoDbClient;


            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);
            var score = _commonLogic.ConvertIdFromGuid(scoreId);

            var updateAt = now.ToUnixTimeMilliseconds();
            var accessText = access switch
            {
                ScoreAccesses.Public => ScoreAccessKind.Public,
                _=>ScoreAccessKind.Private
            };

            var request = new UpdateItemRequest()
            {
                Key = new Dictionary<string, AttributeValue>()
                {
                    [ScoreMainPn.PartitionKey] = new(partitionKey),
                    [ScoreMainPn.SortKey] = new(score),
                },
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    ["#s"] = ScoreMainPn.SortKey,
                    ["#ua"] = ScoreMainPn.UpdateAt,
                    ["#as"] = ScoreMainPn.Access,
                    ["#l"] = ScoreMainPn.Lock,
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    [":as"] = new(accessText),
                    [":ua"] = new(){N = updateAt.ToString()},
                    [":inc"] = new(){N = "1"},
                },
                ConditionExpression = "attribute_exists(#s)",
                UpdateExpression = "SET #ua = :ua, #as = :as ADD #l :inc",
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
