using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemInfoGetter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;

        public ScoreItemInfoGetter(IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;

            var tableName = configuration[EnvironmentNames.ScoreItemDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemDynamoDbTableName}' is not found.");

            TableName = tableName;
        }

        public string TableName { get; set; }

        public async Task<OwnerItemsInfo> GetOwnerItemsInfoAsync(Guid ownerId)
        {
            var itemDataList = await GetItemsAsync(ownerId);

            var totalSize = itemDataList.Sum(x => x.TotalSize);

            var items = itemDataList.Select(ConvertToItemInfo).ToList();

            return new OwnerItemsInfo()
            {
                TotalSize = totalSize,
                ItemInfos = items,
            };
        }

        public async Task<UserItemsInfo> GetUserItemsInfoAsync(Guid ownerId)
        {
            var itemDataList = await GetItemsAsync(ownerId);

            var totalSize = itemDataList.Sum(x => x.TotalSize);

            var items = itemDataList.Select(ConvertToItemInfo).ToList();

            return new UserItemsInfo()
            {
                TotalSize = totalSize,
                ItemInfos = items,
            };
        }

        private ScoreItemInfoBase ConvertToItemInfo(ScoreItemDatabaseItemDataBase itemData)
        {
            if (itemData is ScoreItemDatabaseItemDataImage itemDataImage)
            {
                return new ScoreImageItemInfo()
                {
                    ScoreId = itemDataImage.ScoreId,
                    ItemId = itemDataImage.ItemId,
                    Size = itemDataImage.Size,
                    TotalSize = itemDataImage.TotalSize,
                    ObjectName = itemDataImage.ObjName,
                    OriginalName = itemDataImage.OrgName,
                    Thumbnail = itemDataImage.Thumbnail.ObjName,
                    ThumbnailSize = itemDataImage.Thumbnail.Size,
                };
            }

            throw new ArgumentException();
        }


        public async Task<ScoreItemDatabaseItemDataBase[]> GetItemsAsync(Guid ownerId)
        {
            return await GetAsync(_dynamoDbClient, TableName, ownerId);

            static async Task<ScoreItemDatabaseItemDataBase[]> GetAsync(IAmazonDynamoDB client, string tableName, Guid ownerId)
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
                                [":owner"] = new AttributeValue(partitionKey),
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


        public async Task<ScoreItemDatabaseItemDataBase> GetItemAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {
            return await GetAsync(_dynamoDbClient, TableName, ownerId, scoreId, itemId);

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


    }
}
