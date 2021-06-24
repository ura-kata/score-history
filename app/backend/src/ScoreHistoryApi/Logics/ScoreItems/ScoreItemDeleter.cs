using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemDeleter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public ScoreItemDeleter(IAmazonDynamoDB dynamoDbClient, IAmazonS3 s3Client, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _s3Client = s3Client;
            _configuration = configuration;

            var scoreItemTableName = configuration[EnvironmentNames.ScoreItemDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemDynamoDbTableName}' is not found.");

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");


            var scoreItemS3Bucket = configuration[EnvironmentNames.ScoreItemS3Bucket];
            if (string.IsNullOrWhiteSpace(scoreItemS3Bucket))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreItemS3Bucket}' is not found.");


            ScoreItemTableName = scoreItemTableName;
            ScoreItemRelationTableName = scoreItemRelationTableName;
            ScoreItemS3Bucket = scoreItemS3Bucket;
        }

        public string ScoreItemS3Bucket { get; set; }

        public string ScoreItemRelationTableName { get; set; }

        public string ScoreItemTableName { get; set; }

        public async Task DeleteItemsAsync(Guid ownerId, DeletingScoreItems deletingScoreItems)
        {
            await DeleteItemsFromTableAsync(ownerId, deletingScoreItems);
        }


        public async Task DeleteItemsFromTableAsync(Guid ownerId, DeletingScoreItems deletingScoreItems)
        {
            await CheckDeleteAsync(_dynamoDbClient, ScoreItemRelationTableName, ownerId, deletingScoreItems.ItemIds);

            var itemAndSizeList = await GetItemAndSizeListAsync(_dynamoDbClient, ScoreItemTableName, ownerId, deletingScoreItems.ScoreId);

            var targetHashSet = new HashSet<Guid>();
            foreach (var itemId in deletingScoreItems.ItemIds)
            {
                targetHashSet.Add(itemId);
            }

            var filteredItemAndSizeList = itemAndSizeList
                .Where(x => targetHashSet.Contains(x.itemId))
                .Select(x => (x.itemIdText, x.size))
                .ToArray();

            await DeleteItemsAsync(ownerId, filteredItemAndSizeList);

            static async Task<(string itemIdText, Guid itemId, long size)[]> GetItemAndSizeListAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreItemDatabasePropertyNames.OwnerId,
                        ["#item"] = ScoreItemDatabasePropertyNames.ItemId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                        [":score"] = new AttributeValue(score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#item, :score)",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items
                        .Where(x=>x[ScoreItemDatabasePropertyNames.ItemId].S != ScoreItemDatabaseConstant.ItemIdSummary)
                        .Select(GetItemAndSizeAsync).ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

                static (string itemIdText, Guid itemId, long size) GetItemAndSizeAsync(Dictionary<string, AttributeValue> items)
                {
                    var sizeValue = items[ScoreItemDatabasePropertyNames.Size];
                    var size = long.Parse(sizeValue.N, CultureInfo.InvariantCulture);

                    try
                    {
                        var type = items[ScoreItemDatabasePropertyNames.Type];
                        if (type.S == ScoreItemDatabaseConstant.TypeImage)
                        {
                            var thumbnail = items[ScoreItemDatabasePropertyNames.Thumbnail];
                            var thumbnailSizeValue =
                                thumbnail.M[ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size];
                            size += long.Parse(thumbnailSizeValue.N, CultureInfo.InvariantCulture);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }


                    var itemIdText = items[ScoreItemDatabasePropertyNames.ItemId].S;

                    var item = itemIdText.Substring(ScoreItemDatabaseConstant.ScoreIdLength);
                    var itemId = ScoreDatabaseUtils.ConvertToGuid(item);

                    return (itemIdText, itemId, size);
                }
            }

            static async Task CheckDeleteAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, List<Guid> itemIds)
            {
                var partitionKey = DynamoDbScoreItemRelationUtils.ConvertToPartitionKey(ownerId);

                foreach (var itemId in itemIds)
                {
                    var item = ScoreDatabaseUtils.ConvertToBase64(itemId);
                    var request = new QueryRequest()
                    {
                        TableName = tableName,
                        Limit = 1,
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#owner"] = DynamoDbScoreItemRelationPropertyNames.OwnerId,
                            ["#itemRel"] = DynamoDbScoreItemRelationPropertyNames.ItemRelation,
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                        {
                            [":owner"] = new AttributeValue(partitionKey),
                            [":item"] = new AttributeValue(item),
                        },
                        KeyConditionExpression = "#owner = :owner and begins_with(#itemRel, :item)",
                    };
                    var response = await client.QueryAsync(request);

                    if (0 < response.Count)
                        throw new InvalidOperationException($"'{itemId}' は参照されているため削除できません");
                }

            }
        }

        public async Task DeleteItemsAsync(Guid ownerId, (string itemIdText, long size)[] itemAndSizeList)
        {
            const int chunkSize = 25;

            var chunkList = itemAndSizeList.Select((x, index) => (x, index))
                .GroupBy(x => x.index / chunkSize)
                .Select(x => x.Select(y => y.x).ToArray()).ToArray();


            foreach (var valueTuples in chunkList)
            {
                await DeleteItems25Async(_dynamoDbClient, ScoreItemTableName, ownerId,
                    valueTuples.Select(x => x.itemIdText).ToArray());
                await UpdateSummaryAsync(_dynamoDbClient, ScoreItemTableName, ownerId, valueTuples);
            }

            static async Task DeleteItems25Async(IAmazonDynamoDB client, string tableName, Guid ownerId, string[] items)
            {
                var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);

                var request = new Dictionary<string, List<WriteRequest>>()
                {
                    [tableName] = items.Select(item => new WriteRequest()
                    {
                        DeleteRequest = new DeleteRequest()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(item),
                            }
                        }
                    }).ToList(),
                };
                try
                {
                    var response = await client.BatchWriteItemAsync(request);

                    // TODO 失敗したときのリトライ処理を実装する
                    // response.UnprocessedItems
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            static async Task UpdateSummaryAsync(IAmazonDynamoDB client, string tableName, Guid ownerId,
                (string item, long size)[] itemAndSizeList)
            {
                var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);

                var size = itemAndSizeList.Select(x => x.size).Sum();
                var request = new UpdateItemRequest()
                {
                    TableName = tableName,
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
                        [":size"] = new AttributeValue(){N = "-" + size},
                    },
                    UpdateExpression = "ADD #size :size",
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


        public async Task DeleteAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {
            var size = await GetSizeAsync(_dynamoDbClient, ScoreItemTableName, ownerId, scoreId, itemId);

            await DeleteItemAsync(_dynamoDbClient, ScoreItemTableName, ownerId, scoreId, itemId, size);

            static async Task<long> GetSizeAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, Guid itemId)
            {
                var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
                var item = ScoreDatabaseUtils.ConvertToBase64(itemId);
                try
                {
                    var request = new GetItemRequest()
                    {
                        TableName = tableName,
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                            [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(score + item),
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#totalSize"] = ScoreItemDatabasePropertyNames.TotalSize,
                        },
                        ProjectionExpression = "#totalSize",
                    };
                    var response = await client.GetItemAsync(request);

                    if (!response.IsItemSet)
                    {
                        throw new InvalidOperationException("not found.");
                    }

                    var totalSizeValue = response.Item[ScoreItemDatabasePropertyNames.TotalSize];

                    return long.Parse(totalSizeValue.N);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }


            static async Task DeleteItemAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, Guid itemId, long deleteSize)
            {
                var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
                var item = ScoreDatabaseUtils.ConvertToBase64(itemId);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(score + item),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#item"] = ScoreItemDatabasePropertyNames.ItemId,
                            },
                            ConditionExpression = "attribute_exists(#item)",
                            TableName = tableName,
                        }
                    },
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
                                [":deleteSize"] = new AttributeValue(){N = "-" + deleteSize.ToString()},
                            },
                            UpdateExpression = "ADD #size :deleteSize",
                            TableName = tableName,
                        },
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

        public async Task DeleteOwnerItemsAsync(Guid ownerId)
        {

            var itemAndSizeList = await GetItemAndSizeListAsync(_dynamoDbClient, ScoreItemTableName, ownerId);

            await DeleteItemsAsync(ownerId, itemAndSizeList);

            static async Task<(string itemIdText, long size)[]> GetItemAndSizeListAsync(IAmazonDynamoDB client, string tableName, Guid ownerId)
            {
                var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreItemDatabasePropertyNames.OwnerId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(partitionKey),
                    },
                    KeyConditionExpression = "#owner = :owner",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items
                        .Where(x=>x[ScoreItemDatabasePropertyNames.ItemId].S != ScoreItemDatabaseConstant.ItemIdSummary)
                        .Select(GetItemAndSizeAsync).ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

                static (string itemIdText, long size) GetItemAndSizeAsync(Dictionary<string, AttributeValue> items)
                {
                    var sizeValue = items[ScoreItemDatabasePropertyNames.Size];
                    var size = long.Parse(sizeValue.N, CultureInfo.InvariantCulture);

                    try
                    {
                        var type = items[ScoreItemDatabasePropertyNames.Type];
                        if (type.S == ScoreItemDatabaseConstant.TypeImage)
                        {
                            var thumbnail = items[ScoreItemDatabasePropertyNames.Thumbnail];
                            var thumbnailSizeValue =
                                thumbnail.M[ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size];
                            size += long.Parse(thumbnailSizeValue.N, CultureInfo.InvariantCulture);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }


                    var item = items[ScoreItemDatabasePropertyNames.ItemId].S;

                    return (item, size);
                }
            }
        }

        public async Task<ScoreItemDatabaseItemDataBase> GetItemAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {
            return await GetAsync(_dynamoDbClient, ScoreItemTableName, ownerId, scoreId, itemId);

            static async Task<ScoreItemDatabaseItemDataBase> GetAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId, Guid itemId)
            {
                var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
                var item = ScoreDatabaseUtils.ConvertToBase64(itemId);

                try
                {
                    var request = new GetItemRequest()
                    {
                        TableName = tableName,
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(partitionKey),
                            [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(score + item),
                        },
                    };
                    var response = await client.GetItemAsync(request);

                    if (!response.IsItemSet)
                    {
                        throw new InvalidOperationException("not found.");
                    }

                    var atValue = response.Item[ScoreItemDatabasePropertyNames.At];
                    var sizeValue = response.Item[ScoreItemDatabasePropertyNames.Size];
                    var typeValue = response.Item[ScoreItemDatabasePropertyNames.Type];
                    var itemIdValue = response.Item[ScoreItemDatabasePropertyNames.ItemId];
                    var objNameValue = response.Item[ScoreItemDatabasePropertyNames.ObjName];
                    var ownerIdValue = response.Item[ScoreItemDatabasePropertyNames.OwnerId];
                    var totalSizeValue = response.Item[ScoreItemDatabasePropertyNames.TotalSize];

                    ScoreItemDatabaseItemDataBase result = default;

                    if (typeValue.S == ScoreItemDatabaseConstant.TypeImage)
                    {
                        var orgNameValue = response.Item[ScoreItemDatabasePropertyNames.OrgName];
                        var thumbnailValue = response.Item[ScoreItemDatabasePropertyNames.Thumbnail];

                        var thumbObjNameValue =
                            thumbnailValue.M[ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.ObjName];
                        var thumbSizeValue =
                            thumbnailValue.M[ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size];

                        result = new ScoreItemDatabaseItemDataImage()
                        {
                            OrgName = orgNameValue.S,
                            Thumbnail = new ScoreItemDatabaseItemDataImageThumbnail()
                            {
                                ObjName = thumbObjNameValue.S,
                                Size = long.Parse(thumbSizeValue.N),
                            },
                        };
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    result.Size = long.Parse(sizeValue.N);
                    result.OwnerId = ScoreItemDatabaseUtils.ConvertFromPartitionKey(partitionKey);
                    result.ScoreId = ScoreDatabaseUtils.ConvertToGuid(score);
                    result.ItemId = ScoreDatabaseUtils.ConvertToGuid(item);
                    result.ObjName = objNameValue.S;

                    return result;

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }



        public async Task DeleteObjectAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}/{ScoreItemStorageConstant.FolderName}/{itemId:D}";
            await DeleteObjectsAsync(prefix);
        }

        public async Task DeleteAllScoreObjectAsync(Guid ownerId, Guid scoreId)
        {
            var prefix = $"{ownerId:D}/{scoreId:D}/{ScoreItemStorageConstant.FolderName}";
            await DeleteObjectsAsync(prefix);
        }

        public async Task DeleteAllOwnerObjectAsync(Guid ownerId)
        {
            var prefix = $"{ownerId:D}";
            await DeleteObjectsAsync(prefix);
        }

        public async Task DeleteObjectsAsync(string prefix)
        {
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

            var request = new DeleteObjectsRequest()
            {
                BucketName = ScoreItemS3Bucket,
                Objects = objectKeyList.Select(x=>new KeyVersion()
                {
                    Key = x
                }).ToList(),
            };
            await _s3Client.DeleteObjectsAsync(request);
        }



    }
}
