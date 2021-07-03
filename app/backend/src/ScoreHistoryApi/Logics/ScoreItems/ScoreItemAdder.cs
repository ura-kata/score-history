using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.ScoreItems;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemAdder
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreItemAdder(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IScoreQuota quota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _quota = quota;
            _configuration = configuration;


            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreDataTableName = configuration[EnvironmentNames.ScoreLargeDataDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreLargeDataDynamoDbTableName}' is not found.");
            ScoreDataTableName = scoreDataTableName;


            var scoreItemS3Bucket = configuration[EnvironmentNames.ScoreItemS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreItemS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemS3Bucket}' is not found.");
            ScoreItemS3Bucket = scoreItemS3Bucket;
        }

        public string ScoreItemS3Bucket { get; set; }

        public string ScoreDataTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task<UploadedScoreObjectResult> AddAsync(Guid ownerId, UploadingScoreItem uploadingScoreItem)
        {
            var scoreId = uploadingScoreItem.ScoreId;

            var score = await GetDynamoDbScoreDataAsync(ownerId, scoreId);

            var access = score.score.Access == ScoreDatabaseConstant.ScoreAccessPublic
                ? ScoreObjectAccessControls.Public
                : ScoreObjectAccessControls.Private;

            await using var stream = uploadingScoreItem.Item.OpenReadStream();
            var data = new byte[stream.Length];
            await stream.ReadAsync(data);

            var orgName = uploadingScoreItem.Item.FileName;

            var response = await SaveObjectAsync(ownerId, scoreId, data, access);

            var thumbnail = response.Extra switch
            {
                ImagePngExtra p => new ScoreItemDatabaseItemDataImageThumbnail()
                {
                    Size = p.Thumbnail.Size,
                    ObjName = p.Thumbnail.ObjectName,
                },
                Thumbnail t => new ScoreItemDatabaseItemDataImageThumbnail()
                {
                    Size = t.Size,
                    ObjName = t.ObjectName,
                },
                _ => null,
            };

            var totalSize = response.Size + (thumbnail?.Size ?? 0);

            ScoreItemDatabaseItemDataBase itemData = new ScoreItemDatabaseItemDataImage()
            {
                OwnerId = ownerId,
                ScoreId = scoreId,
                OrgName = orgName,
                ItemId = response.ItemId,
                ObjName = response.ObjectName,
                Size = response.Size,
                Thumbnail = thumbnail,
                TotalSize = totalSize,
            };
            await CreateAsync(itemData);

            return new UploadedScoreObjectResult()
            {
                ImageItemInfo = new ScoreImageItemInfo()
                {
                    Size = itemData.Size,
                    Thumbnail = thumbnail?.ObjName,
                    ObjectName = itemData.ObjName,
                    ItemId = itemData.ItemId,
                    OriginalName = orgName,
                    ScoreId = scoreId,
                    TotalSize = itemData.TotalSize,
                    ThumbnailSize = thumbnail?.Size ?? 0,
                },
            };
        }


        public async Task CreateAsync(ScoreItemDatabaseItemDataBase itemData)
        {

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            var maxSize = _quota.OwnerItemMaxSize;

            await PutDataAsync(_dynamoDbClient, ScoreTableName, itemData, maxSize, now);

            static async Task PutDataAsync(
                IAmazonDynamoDB client,
                string tableName,
                ScoreItemDatabaseItemDataBase itemData,
                long maxSize,
                DateTimeOffset now)
            {
                var (items, partitionKey, _, item, totalSize) = ScoreItemDatabaseUtils.CreateDynamoDbValue(itemData, now);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(ScoreItemDatabaseConstant.ItemIdSummary),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#size"] = ScoreItemDatabasePropertyNames.Size,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":itemSize"] = new AttributeValue(){N = totalSize.ToString()},
                                [":maxSize"] = new AttributeValue()
                                {
                                    N = (maxSize - totalSize).ToString()
                                },
                            },
                            ConditionExpression = "#size < :maxSize",
                            UpdateExpression = "ADD #size :itemSize",
                            TableName = tableName,
                        },
                    },
                    new TransactWriteItem()
                    {
                        Put = new Put()
                        {
                            Item = items,
                            TableName = tableName,
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#item"] = ScoreItemDatabasePropertyNames.ItemId,
                            },
                            ConditionExpression = "attribute_not_exists(#item)",
                        }
                    },
                };
                try
                {
                    await client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                    {
                        TransactItems = actions,
                        ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
                    });
                }
                catch (ResourceNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (InternalServerErrorException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (TransactionCanceledException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }


        public async Task<SavedItemData> SaveObjectAsync(
            Guid ownerId, Guid scoreId, byte[] data,
            ScoreObjectAccessControls accessControl)
        {
            return await SaveObjectAsync(ownerId, scoreId, Guid.NewGuid(), data, accessControl);
        }
        public async Task<SavedItemData> SaveObjectAsync(Guid ownerId, Guid scoreId, Guid itemId, byte[] data, ScoreObjectAccessControls accessControl)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            var itemType = ScoreItemStorageUtils.CheckItemType(data);

            if (itemType == ItemTypes.None)
                throw new NotSupportedItemFileException($"Not supported file. top 10 bytes : '{string.Join(" ",data.Take(10).Select(b=>b.ToString("X2")))}'");

            var keyDir = $"{ownerId:D}/{scoreId:D}/{ScoreItemStorageConstant.FolderName}/{itemId:D}/";


            await using var srcStream = new MemoryStream(data);

            using var thumbnailImage = await Image.LoadAsync(srcStream);
            srcStream.Seek(0, SeekOrigin.Begin);


            var height = (int) (thumbnailImage.Height * (double) ScoreItemStorageConstant.ThumbnailMaxWidthPixel / thumbnailImage.Width);
            thumbnailImage.Mutate(x=>x.Resize(ScoreItemStorageConstant.ThumbnailMaxWidthPixel,height));

            await using var thumbnailStream = new MemoryStream();

            await thumbnailImage.SaveAsJpegAsync(thumbnailStream);
            thumbnailStream.Seek(0, SeekOrigin.Begin);

            var thumbnailSize = thumbnailStream.Length;

            var objectFileName = ScoreItemStorageUtils.GetFileName(itemType);
            var thumbnailFileName = ScoreItemStorageConstant.ThumbnailFileName;

            var acl = accessControl switch
            {
                ScoreObjectAccessControls.Private => S3CannedACL.Private,
                ScoreObjectAccessControls.Public => S3CannedACL.PublicRead,
                _ => throw new NotSupportedException(),
            };

            var objectSaveRequest = new PutObjectRequest()
            {
                BucketName = ScoreItemS3Bucket,
                Key = keyDir + objectFileName,
                CannedACL = acl,
                InputStream = srcStream,
            };

            var thumbnailSaveRequest = new PutObjectRequest()
            {
                BucketName = ScoreItemS3Bucket,
                Key = keyDir + thumbnailFileName,
                CannedACL = acl,
                InputStream = thumbnailStream,
            };

            await _s3Client.PutObjectAsync(thumbnailSaveRequest);
            await _s3Client.PutObjectAsync(objectSaveRequest);

            return new SavedItemData()
            {
                Data = data,
                OwnerId = ownerId,
                ScoreId = scoreId,
                Type = itemType,
                ItemId = itemId,
                ObjectName = objectFileName,
                Size = data.Length,
                Extra = new Thumbnail()
                {
                    ObjectName = thumbnailFileName,
                    Size = thumbnailSize,
                },
                AccessControl = ScoreObjectAccessControls.Private
            };
        }

        public async Task<(DynamoDbScore score, Dictionary<string, string> hashSet)> GetDynamoDbScoreDataAsync(
            Guid ownerId, Guid scoreId)
        {

            var dynamoDbScore = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);
            var hashSet = await GetAnnotationsAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId);

            var descriptionHash = dynamoDbScore.Data.GetDescriptionHash();
            var (success, description) =
                await TryGetDescriptionAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId, descriptionHash);

            if (success)
            {
                hashSet[descriptionHash] = description;
            }

            return (dynamoDbScore, hashSet);

            static async Task<DynamoDbScore> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid scoreId)
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.SortKey] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);

                if (!response.IsItemSet)
                {
                    throw new NotFoundScoreException("Not found score.");
                }

                var dynamoDbScore = new DynamoDbScore(response.Item);

                return dynamoDbScore;
            }


            static async Task<Dictionary<string, string>> GetAnnotationsAsync(
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScoreDataPropertyNames.OwnerId,
                        ["#data"] = DynamoDbScoreDataPropertyNames.DataId,
                        ["#content"] = DynamoDbScoreDataPropertyNames.Content,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                        [":annScore"] = new AttributeValue(DynamoDbScoreDataConstant.PrefixAnnotation + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#data, :annScore)",
                    ProjectionExpression = "#data, #content",
                };

                try
                {
                    var response = await client.QueryAsync(request);

                    var result = new Dictionary<string, string>();
                    var substringStartIndex = DynamoDbScoreDataConstant.PrefixAnnotation.Length + score.Length;
                    foreach (var item in response.Items)
                    {
                        var hashValue = item[DynamoDbScoreDataPropertyNames.DataId];
                        var hash = hashValue.S.Substring(substringStartIndex);
                        var contentValue = item[DynamoDbScoreDataPropertyNames.Content];
                        result[hash] = contentValue.S;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

            }

            static async Task<(bool success,string description)> TryGetDescriptionAsync(
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, string descriptionHash)
            {
                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                        [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(DynamoDbScoreDataConstant.PrefixDescription + score + descriptionHash),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#content"] = DynamoDbScoreDataPropertyNames.Content,

                    },
                    ProjectionExpression = "#content",
                };

                try
                {
                    var response = await client.GetItemAsync(request);

                    if (!response.IsItemSet)
                    {
                        return (false, "");
                    }

                    var description = response.Item[DynamoDbScoreDataPropertyNames.Content].S;
                    return (true, description);
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
