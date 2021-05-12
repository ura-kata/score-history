using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics
{
    public static class ScoreItemDatabaseConstant
    {
        public const string TypeImage = "image";

        public const string AccessPublic = "public";
        public const string AccessPrivate = "private";

        public const string ItemIdSummary = "summary";
    }

    public static class ScoreItemDatabasePropertyNames
    {
        public const string OwnerId = "owner";
        public const string ItemId = "item";
        public const string ObjName = "obj_name";
        public const string Size = "size";
        public const string At = "at";
        public const string Type = "type";

        public const string OrgName = "org_name";
        public const string Thumbnail = "thumbnail";
        public static class ThumbnailPropertyNames
        {
            public const string ObjName = "obj_name";
            public const string Size = "size";
        }
    }

    public abstract class ScoreItemDatabaseItemDataBase
    {
        public Guid OwnerId { get; set; }
        public Guid ScoreId { get; set; }
        public Guid ItemId { get; set; }
        public string ObjName { get; set; }
        public long Size { get; set; }
    }

    public class ScoreItemDatabaseItemDataImageThumbnail
    {
        public string ObjName { get; set;}
        public long Size { get; set;}
    }

    public class ScoreItemDatabaseItemDataImage: ScoreItemDatabaseItemDataBase
    {
        public string OrgName { get; set; }
        public ScoreItemDatabaseItemDataImageThumbnail Thumbnail { get; set; }
    }

    public static class ScoreItemDatabaseUtils
    {
        public static (Dictionary<string, AttributeValue> items, string owner) CreateDynamoDbValue(ScoreItemDatabaseItemDataBase itemData, DateTimeOffset now)
        {
            var items = new Dictionary<string, AttributeValue>();

            var owner = ScoreDatabaseUtils.ConvertToBase64(itemData.OwnerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(itemData.ScoreId);
            var item = ScoreDatabaseUtils.ConvertToBase64(itemData.ItemId);
            var at = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

            items[ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner);
            items[ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(score + item);
            items[ScoreItemDatabasePropertyNames.ObjName] = new AttributeValue(itemData.ObjName);
            items[ScoreItemDatabasePropertyNames.Size] = new AttributeValue() {N = itemData.Size.ToString()};
            items[ScoreItemDatabasePropertyNames.At] = new AttributeValue(at);

            if ( itemData is ScoreItemDatabaseItemDataImage itemDataImage)
            {
                items[ScoreItemDatabasePropertyNames.Type] = new AttributeValue(ScoreItemDatabaseConstant.TypeImage);

                items[ScoreItemDatabasePropertyNames.OrgName] = new AttributeValue(itemDataImage.OrgName);

                items[ScoreItemDatabasePropertyNames.Thumbnail] = new AttributeValue()
                {
                    M = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.ObjName] =
                            new AttributeValue(itemDataImage.Thumbnail.ObjName),
                        [ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size] = new AttributeValue()
                            {N = itemDataImage.Thumbnail.Size.ToString()},
                    }
                };
            }

            return (items, owner);
        }

        public static long GetSize(ScoreItemDatabaseItemDataBase itemData)
        {
            long size = itemData.Size;

            if (itemData is ScoreItemDatabaseItemDataImage itemDataImage)
            {
                size += itemDataImage.Thumbnail.Size;
            }

            return size;
        }
    }

    /// <summary>
    /// 楽譜アイテムデータベース
    /// </summary>
    public interface IScoreItemDatabase
    {
        /// <summary>
        /// データベースを初期化する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        Task InitializeAsync(Guid ownerId);

        /// <summary>
        /// 楽譜のアイテムを作成する
        /// </summary>
        /// <param name="itemData"></param>
        /// <returns></returns>
        Task CreateAsync(ScoreItemDatabaseItemDataBase itemData);

        /// <summary>
        /// 楽譜のアイテムを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task DeleteAsync(Guid ownerId, Guid scoreId, Guid itemId);

        /// <summary>
        /// 楽譜のアイテムを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <returns></returns>
        Task DeleteScoreItemsAsync(Guid ownerId, Guid scoreId);

        /// <summary>
        /// 楽譜のアイテムを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        Task DeleteOwnerItemsAsync(Guid ownerId);
    }

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
                        [ScoreItemDatabasePropertyNames.Size] = new AttributeValue(){N = "0"}
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
                var (items, owner) = ScoreItemDatabaseUtils.CreateDynamoDbValue(itemData, now);
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
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":itemSize"] = new AttributeValue(){N = itemSize.ToString()},
                                [":maxSize"] = new AttributeValue()
                                {
                                    N = (maxSize - itemSize).ToString()
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

        public Task DeleteAsync(Guid ownerId, Guid scoreId, Guid itemId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteScoreItemsAsync(Guid ownerId, Guid scoreId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteOwnerItemsAsync(Guid ownerId)
        {
            throw new NotImplementedException();
        }
    }
}
