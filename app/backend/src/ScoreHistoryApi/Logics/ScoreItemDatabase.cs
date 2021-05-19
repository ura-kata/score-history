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

namespace ScoreHistoryApi.Logics
{
    public class ScoreItemDatabase : IScoreItemDatabase
    {
        private readonly IScoreQuota _quota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        public string TableName { get; }

        public ScoreItemDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            var tableName = configuration[EnvironmentNames.ScoreItemDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemDynamoDbTableName}' is not found.");

            TableName = tableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }
        public ScoreItemDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient,string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException(nameof(tableName));

            TableName = tableName;
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
                        ["#owner"] = ScoreDatabasePropertyNames.OwnerId
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
                var (items, owner, _, item) = ScoreItemDatabaseUtils.CreateDynamoDbValue(itemData, now);
                var itemSize = ScoreItemDatabaseUtils.GetSize(itemData);

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
                                ["#itemList"] = ScoreItemDatabasePropertyNames.ItemList,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":itemSize"] = new AttributeValue(){N = itemSize.ToString()},
                                [":maxSize"] = new AttributeValue()
                                {
                                    N = (maxSize - itemSize).ToString()
                                },
                                [":itemList"] = new AttributeValue()
                                {
                                    SS = new List<string>(){item},
                                },
                            },
                            ConditionExpression = "#size < :maxSize",
                            UpdateExpression = "ADD #size :itemSize, #itemList :itemList",
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

        public async Task DeleteAsync(Guid ownerId, Guid itemId)
        {

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var item = ScoreDatabaseUtils.ConvertToBase64(itemId);

            var size = await GetSizeAsync(_dynamoDbClient, TableName, owner, item);

            await DeleteItemAsync(_dynamoDbClient, TableName, owner, item, size);

            static async Task<long> GetSizeAsync(IAmazonDynamoDB client, string tableName, string owner, string item)
            {
                try
                {
                    var request = new GetItemRequest()
                    {
                        TableName = tableName,
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                            [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(item),
                        },
                    };
                    var response = await client.GetItemAsync(request);

                    var sizeValue = response.Item[ScoreItemDatabasePropertyNames.Size];
                    if (sizeValue is null)
                        throw new InvalidOperationException("not found.");

                    var size = long.Parse(sizeValue.N, CultureInfo.InvariantCulture);

                    var type = response.Item[ScoreItemDatabasePropertyNames.Type];
                    if (type.S == ScoreItemDatabaseConstant.TypeImage)
                    {
                        var thumbnail = response.Item[ScoreItemDatabasePropertyNames.Thumbnail];
                        var thumbnailSizeValue = thumbnail.M[ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size];
                        size += long.Parse(thumbnailSizeValue.N, CultureInfo.InvariantCulture);
                    }

                    return size;

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }


            static async Task DeleteItemAsync(IAmazonDynamoDB client, string tableName, string owner, string item, long deleteSize)
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
                                [ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(item),
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
                                ["#itemList"] = ScoreItemDatabasePropertyNames.ItemList,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":deleteSize"] = new AttributeValue(){N = "-" + deleteSize.ToString()},
                                [":itemList"] = new AttributeValue(){SS = new List<string>(){item}},
                            },
                            UpdateExpression = "ADD #size :deleteSize DELETE #itemList :itemList",
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
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

            var itemAndSizeList = await GetItemAndSizeListAsync(_dynamoDbClient, TableName, owner);

            await DeleteItemsAsync(owner, itemAndSizeList);

            static async Task<(string item, long size)[]> GetItemAndSizeListAsync(IAmazonDynamoDB client, string tableName, string owner)
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


        public async Task DeleteItemsAsync(string owner, (string item, long size)[] itemAndSizeList)
        {
            const int chunkSize = 25;

            var chunkList = itemAndSizeList.Select((x, index) => (x, index))
                .GroupBy(x => x.index / chunkSize)
                .Select(x => x.Select(y => y.x).ToArray()).ToArray();


            foreach (var valueTuples in chunkList)
            {
                await DeleteItems25Async(_dynamoDbClient, TableName, owner,
                    valueTuples.Select(x => x.item).ToArray());
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
                    await client.BatchWriteItemAsync(request);
                    // TODO 失敗したときのリトライ処理を実装する
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
                        ["#itemList"] = ScoreItemDatabasePropertyNames.ItemList,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":size"] = new AttributeValue(){N = "-" + size},
                        [":itemList"] = new AttributeValue(){SS = itemAndSizeList.Select(x=>x.item).ToList()},
                    },
                    UpdateExpression = "ADD #size :size DELETE #itemList :itemList",
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
