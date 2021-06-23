using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class ScoreItemDeleter
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IConfiguration _configuration;

        public ScoreItemDeleter(IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _configuration = configuration;

            var scoreItemTableName = configuration[EnvironmentNames.ScoreItemDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemDynamoDbTableName}' is not found.");

            var scoreItemRelationTableName = configuration[EnvironmentNames.ScoreItemRelationDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreItemRelationTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreItemRelationDynamoDbTableName}' is not found.");

            ScoreItemTableName = scoreItemTableName;
            ScoreItemRelationTableName = scoreItemRelationTableName;
        }

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

    }
}
