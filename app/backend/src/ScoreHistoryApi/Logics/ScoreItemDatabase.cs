using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics
{
    public class ScoreItemDatabase : IScoreItemDatabase
    {
        private readonly IScoreQuota _quota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        public string TableName { get; }
        public string ScoreItemRelationTableName { get; }

        public ScoreItemDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            var tableName = configuration[EnvironmentNames.ScoreItemDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemDynamoDbTableName}' is not found.");

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");

            TableName = tableName;
            ScoreItemRelationTableName = scoreItemRelationTableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }
        public ScoreItemDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient,string tableName,string scoreItemRelationTableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException(nameof(tableName));
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new ArgumentException(nameof(scoreItemRelationTableName));

            TableName = tableName;
            ScoreItemRelationTableName = scoreItemRelationTableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task InitializeAsync(Guid ownerId)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

            await PutAsync(_dynamoDbClient, TableName, owner);

            static async Task PutAsync(IAmazonDynamoDB client, string tableName, string owner)
            {
                var request = new PutItemRequest()
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(ScoreItemDatabaseConstant.ItemIdSummary),
                        [ScoreItemDatabasePropertyNames.Size] = new AttributeValue(){N = "0"},
                    },
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId
                    },
                    ConditionExpression = "attribute_not_exists(#owner)",
                };
                try
                {
                    await client.PutItemAsync(request);
                }
                catch (ConditionalCheckFailedException ex)
                {
                    if (ex.ErrorCode == "ConditionalCheckFailedException")
                    {
                        throw new AlreadyInitializedException(ex);
                    }
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        public async Task CreateAsync(ScoreItemDatabaseItemDataBase itemData)
        {

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            var maxSize = _quota.OwnerItemMaxSize;

            await PutDataAsync(_dynamoDbClient, TableName, itemData, maxSize, now);

            static async Task PutDataAsync(
                IAmazonDynamoDB client,
                string tableName,
                ScoreItemDatabaseItemDataBase itemData,
                long maxSize,
                DateTimeOffset now)
            {
                var (items, owner, _, item, totalSize) = ScoreItemDatabaseUtils.CreateDynamoDbValue(itemData, now);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
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

        public async Task DeleteAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
            var item = ScoreDatabaseUtils.ConvertToBase64(itemId);

            var size = await GetSizeAsync(_dynamoDbClient, TableName, owner, score, item);

            await DeleteItemAsync(_dynamoDbClient, TableName, owner, score, item, size);

            static async Task<long> GetSizeAsync(IAmazonDynamoDB client, string tableName, string owner, string score, string item)
            {
                try
                {
                    var request = new GetItemRequest()
                    {
                        TableName = tableName,
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
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


            static async Task DeleteItemAsync(IAmazonDynamoDB client, string tableName, string owner, string score, string item, long deleteSize)
            {
                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
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
                                [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
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

        public async Task DeleteItemsAsync(Guid ownerId, DeletingScoreItems deletingScoreItems)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(deletingScoreItems.ScoreId);

            await CheckDeleteAsync(_dynamoDbClient, ScoreItemRelationTableName, owner, deletingScoreItems.ItemIds);

            var itemAndSizeList = await GetItemAndSizeListAsync(_dynamoDbClient, TableName, owner, score);

            var targetHashSet = new HashSet<Guid>();
            foreach (var itemId in deletingScoreItems.ItemIds)
            {
                targetHashSet.Add(itemId);
            }

            var filteredItemAndSizeList = itemAndSizeList
                .Where(x => targetHashSet.Contains(x.itemId))
                .Select(x => (x.itemIdText, x.size))
                .ToArray();

            await DeleteItemsAsync(owner, filteredItemAndSizeList);

            static async Task<(string itemIdText, Guid itemId, long size)[]> GetItemAndSizeListAsync(IAmazonDynamoDB client, string tableName, string owner, string score)
            {
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
                        [":owner"] = new AttributeValue(owner),
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

            static async Task CheckDeleteAsync(IAmazonDynamoDB client, string tableName, string owner, List<Guid> itemIds)
            {
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
                            [":owner"] = new AttributeValue(owner),
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

        public async Task DeleteOwnerItemsAsync(Guid ownerId)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

            var itemAndSizeList = await GetItemAndSizeListAsync(_dynamoDbClient, TableName, owner);

            await DeleteItemsAsync(owner, itemAndSizeList);

            static async Task<(string itemIdText, long size)[]> GetItemAndSizeListAsync(IAmazonDynamoDB client, string tableName, string owner)
            {
                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreItemDatabasePropertyNames.OwnerId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner),
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
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
            var item = ScoreDatabaseUtils.ConvertToBase64(itemId);

            return await GetAsync(_dynamoDbClient, TableName, owner, score, item);

            static async Task<ScoreItemDatabaseItemDataBase> GetAsync(IAmazonDynamoDB client, string tableName, string owner, string score, string item)
            {
                try
                {
                    var request = new GetItemRequest()
                    {
                        TableName = tableName,
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
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
                    result.OwnerId = ScoreDatabaseUtils.ConvertToGuid(owner);
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

        public async Task<ScoreItemDatabaseItemDataBase[]> GetItemsAsync(Guid ownerId)
        {
             var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

            return await GetAsync(_dynamoDbClient, TableName, owner);

            static async Task<ScoreItemDatabaseItemDataBase[]> GetAsync(IAmazonDynamoDB client, string tableName, string owner)
            {
                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreItemDatabasePropertyNames.OwnerId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner),
                    },
                    KeyConditionExpression = "#owner = :owner",
                    Limit = 500,
                };

                try
                {
                    var response = await client.QueryAsync(request);

                    var items = response.Items.ToList();

                    while(0 < response.LastEvaluatedKey?.Count)
                    {
                        var nextRequest = new QueryRequest()
                        {
                            TableName = tableName,
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#owner"] = ScoreItemDatabasePropertyNames.OwnerId,
                                ["#item"] = ScoreItemDatabasePropertyNames.ItemId,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":owner"] = new AttributeValue(owner),
                                [":item"] = new AttributeValue(response
                                    .LastEvaluatedKey[ScoreItemDatabasePropertyNames.ItemId].S),
                            },
                            KeyConditionExpression = "#owner = :owner and :item < #item",
                            Limit = 500,
                        };

                        var nextResponse = await client.QueryAsync(nextRequest);
                        items.AddRange(nextResponse.Items);

                        response = nextResponse;
                    }

                    return items
                        .Where(x=>x[ScoreItemDatabasePropertyNames.ItemId].S != ScoreItemDatabaseConstant.ItemIdSummary)
                        .Select(ScoreItemDatabaseUtils.ConvertFromDynamoDbValue)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }


        public async Task DeleteItemsAsync(string owner, (string itemIdText, long size)[] itemAndSizeList)
        {
            const int chunkSize = 25;

            var chunkList = itemAndSizeList.Select((x, index) => (x, index))
                .GroupBy(x => x.index / chunkSize)
                .Select(x => x.Select(y => y.x).ToArray()).ToArray();


            foreach (var valueTuples in chunkList)
            {
                await DeleteItems25Async(_dynamoDbClient, TableName, owner,
                    valueTuples.Select(x => x.itemIdText).ToArray());
                await UpdateSummaryAsync(_dynamoDbClient, TableName, owner, valueTuples);
            }

            static async Task DeleteItems25Async(IAmazonDynamoDB client, string tableName, string owner, string[] items)
            {
                var request = new Dictionary<string, List<WriteRequest>>()
                {
                    [tableName] = items.Select(item => new WriteRequest()
                    {
                        DeleteRequest = new DeleteRequest()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
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

            static async Task UpdateSummaryAsync(IAmazonDynamoDB client, string tableName, string owner,
                (string item, long size)[] itemAndSizeList)
            {
                var size = itemAndSizeList.Select(x => x.size).Sum();
                var request = new UpdateItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
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
    }
}
