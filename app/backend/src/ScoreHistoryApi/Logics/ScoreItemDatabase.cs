using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
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
