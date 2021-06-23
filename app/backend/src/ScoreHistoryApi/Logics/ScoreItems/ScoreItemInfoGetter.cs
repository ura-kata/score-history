using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
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
    }
}
