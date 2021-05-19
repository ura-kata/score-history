using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.Exceptions;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;


// TODO 変更時にハッシュを確認してから更新するようにする処理を追加する

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜のデータベース
    /// </summary>
    public class ScoreDatabase : IScoreDatabase
    {
        private readonly IScoreQuota _quota;
        private readonly IAmazonDynamoDB _dynamoDbClient;
        public string ScoreTableName { get; } = "ura-kata-score-history";
        public string ScoreDataTableName { get; } = "ura-kata-score-history-data";

        public ScoreDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient, IConfiguration configuration)
        {
            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreDataTableName = configuration[EnvironmentNames.ScoreDataDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDataDynamoDbTableName}' is not found.");
            ScoreDataTableName = scoreDataTableName;

            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }
        public ScoreDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient,string scoreTableName,string scoreDataTableName)
        {
            if (string.IsNullOrWhiteSpace(scoreTableName))
                throw new ArgumentException(nameof(scoreTableName));
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new ArgumentException(nameof(scoreDataTableName));

            ScoreTableName = scoreTableName;
            ScoreDataTableName = scoreDataTableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task InitializeAsync(Guid ownerId)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

            await PutAsync(_dynamoDbClient, ScoreTableName, owner);

            static async Task PutAsync(IAmazonDynamoDB client, string tableName, string owner)
            {
                var request = new PutItemRequest()
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                        [DynamoDbScorePropertyNames.ScoreCount] = new AttributeValue(){N = "0"}
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

        public async Task CreateAsync(Guid ownerId, string title, string description)
        {
            var newScoreId = Guid.NewGuid();
            await CreateAsync(ownerId, newScoreId, title, description);
        }
        public async Task CreateAsync(Guid ownerId, Guid newScoreId, string title, string description)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var preprocessingTitle = title.Trim();
            if (preprocessingTitle == "")
                throw new ArgumentException(nameof(title));

            var scoreCountMax = _quota.ScoreCountMax;

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await PutScoreAsync(
                _dynamoDbClient, ScoreTableName, ownerId, newScoreId, scoreCountMax,
                title, description, now);

            static async Task PutScoreAsync(
                IAmazonDynamoDB client,
                string tableName,
                Guid ownerId,
                Guid newScoreId,
                int maxCount,
                string title,
                string description,
                DateTimeOffset now)
            {
                var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
                var newScore = ScoreDatabaseUtils.ConvertToBase64(newScoreId);

                var data = new DynamoDbScoreDataV1()
                {
                    Title = title,
                    DescriptionHash = description,
                };
                var dataAttributeValue = data.ConvertToAttributeValue();
                var dataHash = ScoreDatabaseUtils.CalcHash(data);
                var createAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#count"] = DynamoDbScorePropertyNames.ScoreCount,
                                ["#scores"] = DynamoDbScorePropertyNames.Scores,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "1"},
                                [":countMax"] = new AttributeValue()
                                {
                                    N = maxCount.ToString()
                                },
                                [":newScore"] = new AttributeValue()
                                {
                                    SS = new List<string>(){newScore}
                                }
                            },
                            ConditionExpression = "#count < :countMax",
                            UpdateExpression = "ADD #count :increment, #scores :newScore",
                            TableName = tableName,
                        },
                    },
                    new TransactWriteItem()
                    {
                        Put = new Put()
                        {
                            Item = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + newScore),
                                [DynamoDbScorePropertyNames.DataHash] = new AttributeValue(dataHash),
                                [DynamoDbScorePropertyNames.CreateAt] = new AttributeValue(createAt),
                                [DynamoDbScorePropertyNames.UpdateAt] = new AttributeValue(updateAt),
                                [DynamoDbScorePropertyNames.Access] = new AttributeValue(ScoreDatabaseConstant.ScoreAccessPrivate),
                                [DynamoDbScorePropertyNames.SnapshotCount] = new AttributeValue(){N = "0"},
                                [DynamoDbScorePropertyNames.Data] = dataAttributeValue,
                            },
                            TableName = tableName,
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                            },
                            ConditionExpression = "attribute_not_exists(#score)",
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
                    var updateReason = ex.CancellationReasons[0];

                    if (updateReason.Code == "ConditionalCheckFailed")
                    {
                        var request = new GetItemRequest()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                            },
                        };
                        var checkResponse = await client.GetItemAsync(request);

                        if (checkResponse.Item.TryGetValue(DynamoDbScorePropertyNames.ScoreCount, out _))
                        {
                            throw new CreatedScoreException(CreatedScoreExceptionCodes.ExceededUpperLimit, ex);
                        }
                        else
                        {
                            throw new UninitializedScoreException(ex);
                        };
                    }

                    var putReason = ex.CancellationReasons[1];

                    if (putReason.Code == "ConditionalCheckFailed")
                    {
                        throw new ExistedScoreException(ex);
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

        public async Task DeleteAsync(Guid ownerId, Guid scoreId)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            await DeleteMainAsync(_dynamoDbClient, ScoreTableName, owner, score);

            // スナップショットの削除は SQS を使ったほうがいいかも
            var snapshotScoreIds = await GetSnapshotScoreIdsAsync(_dynamoDbClient, ScoreTableName, owner, score);

            await DeleteSnapshotsAsync(_dynamoDbClient, ScoreTableName, owner, snapshotScoreIds);

            var dataIds = await GetScoreDataIdsAsync(_dynamoDbClient, ScoreDataTableName, owner, score);

            await DeleteDataAsync(_dynamoDbClient, ScoreDataTableName, owner, score, dataIds);

            static async Task DeleteMainAsync(IAmazonDynamoDB client, string tableName, string owner, string score)
            {
                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                            },
                            ConditionExpression = "attribute_exists(#score)",
                            TableName = tableName,
                        }
                    },
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#count"] = DynamoDbScorePropertyNames.ScoreCount,
                                ["#scores"] = DynamoDbScorePropertyNames.Scores,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "-1"},
                                [":score"] = new AttributeValue()
                                {
                                    SS = new List<string>(){score}
                                }
                            },
                            UpdateExpression = "ADD #count :increment DELETE #scores :score",
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
                    var deleteReason = ex.CancellationReasons[0];

                    if (deleteReason.Code == "ConditionalCheckFailed")
                    {
                        throw new NotFoundScoreException(ex);
                    }
                    var updateReason = ex.CancellationReasons[1];

                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            static async Task<string[]> GetSnapshotScoreIdsAsync(IAmazonDynamoDB client, string tableName, string owner, string score)
            {
                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId,
                        ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner),
                        [":snapPrefix"] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#score, :snapPrefix)",
                    ProjectionExpression = "#score",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items.Select(x => x[DynamoDbScorePropertyNames.ScoreId]?.S)
                        .Where(x => !(x is null))
                        .ToArray();
                }
                catch (InternalServerErrorException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ProvisionedThroughputExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (RequestLimitExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ResourceNotFoundException ex)
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


            static async Task DeleteSnapshotsAsync(IAmazonDynamoDB client, string tableName, string owner, string[] scoreIds)
            {
                const int chunkSize = 25;

                var chunkList = scoreIds.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await DeleteSnapshot25Async(client, tableName, owner, ids);
                }

                static async Task DeleteSnapshot25Async(IAmazonDynamoDB client, string tableName, string owner, string[] scoreIds)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = scoreIds.Select(scoreId=>new WriteRequest()
                        {
                            DeleteRequest = new DeleteRequest()
                            {
                                Key = new Dictionary<string, AttributeValue>()
                                {
                                    [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                    [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(scoreId),
                                }
                            }
                        }).ToList(),
                    };

                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 削除時に失敗したデータを取得しリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            }

            static async Task<string[]> GetScoreDataIdsAsync(IAmazonDynamoDB client, string tableName, string owner, string score)
            {
                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreDataDatabasePropertyNames.OwnerId,
                        ["#data"] = ScoreDataDatabasePropertyNames.DataId,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner + score),
                    },
                    KeyConditionExpression = "#owner = :owner",
                    ProjectionExpression = "#data",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    return response.Items.Select(x => x[ScoreDataDatabasePropertyNames.DataId]?.S)
                        .Where(x => !(x is null))
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }

            static async Task DeleteDataAsync(IAmazonDynamoDB client, string tableName, string owner, string score, string[] dataIds)
            {
                const int chunkSize = 25;

                var chunkList = dataIds.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x=>x.Select(y=>y.x).ToArray())
                    .ToArray();

                foreach (var ids in chunkList)
                {
                    await DeleteSnapshot25Async(client, tableName, owner, score, ids);
                }

                static async Task DeleteSnapshot25Async(IAmazonDynamoDB client, string tableName, string owner, string score, string[] dataIds)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = dataIds.Select(dataId=>new WriteRequest()
                        {
                            DeleteRequest = new DeleteRequest()
                            {
                                Key = new Dictionary<string, AttributeValue>()
                                {
                                    [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner + score),
                                    [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(dataId),
                                }
                            }
                        }).ToList(),
                    };

                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 削除時に失敗したデータを取得しリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            }
        }

        public async Task UpdateTitleAsync(Guid ownerId, Guid scoreId, string title)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.Title = title;

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();
            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, title, newHash, oldHash, now);

            static async Task<(DynamoDbScoreDataV1 data,string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result,hash);
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string newTitle,
                string newHash,
                string oldHash,
                DateTimeOffset now
                )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#title"] = DynamoDbScorePropertyNames.DataPropertyNames.Title,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newTitle"] = new AttributeValue(newTitle),
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression = "SET #updateAt = :updateAt, #hash = :newHash, #data.#title = :newTitle",
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

        public async Task UpdateDescriptionAsync(Guid ownerId, Guid scoreId, string description)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.DescriptionHash = description;

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();
            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, description, newHash, oldHash, now);

            static async Task<(DynamoDbScoreDataV1 data,string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
        {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result,hash);
        }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string newDescription,
                string newHash,
                string oldHash,
                DateTimeOffset now
                )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#desc"] = DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newDesc"] = new AttributeValue(newDescription),
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression = "SET #updateAt = :updateAt, #hash = :newHash, #data.#desc = :newDesc",
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

        public async Task AddPagesAsync(Guid ownerId, Guid scoreId, List<NewScorePage> pages)
        {
            if (pages.Count == 0)
                throw new ArgumentException(nameof(pages));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.Page ??= new List<DynamoDbScorePageV1>();

            var newPages = new List<DynamoDbScorePageV1>();

            var pageId = data.Page.Count == 0 ? 0 : data.Page.Select(x => x.Id).Max() + 1;
            foreach (var page in pages)
            {
                var p = new DynamoDbScorePageV1()
                {
                    Id = pageId++,
                    ItemId = ScoreDatabaseUtils.ConvertToBase64(page.ItemId),
                    Page = page.Page,
                };
                newPages.Add(p);
                data.Page.Add(p);
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            // TODO ページの追加上限値判定を追加
            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, newPages, newHash, oldHash, now);

            static async Task<(DynamoDbScoreDataV1 data,string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result,hash);
            }

            static AttributeValue ConvertFromPages(List<DynamoDbScorePageV1> pages)
            {
                var result = new AttributeValue()
                {
                    L = new List<AttributeValue>()
                };

                foreach (var page in pages)
                {
                    var p = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Id] = new AttributeValue()
                        {
                            N = page.Id.ToString(),
                        }
                    };
                    if (page.Page != null)
                    {
                        p[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Page] = new AttributeValue(page.Page);
                    }
                    if (page.ItemId != null)
                    {
                        p[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.ItemId] = new AttributeValue(page.ItemId);
                    }
                    if(p.Count == 0)
                        continue;

                    result.L.Add(new AttributeValue() {M = p});
                }

                return result;
            }
            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                List<DynamoDbScorePageV1> newPages,
                string newHash,
                string oldHash,
                DateTimeOffset now
                )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#pages"] = DynamoDbScorePropertyNames.DataPropertyNames.Pages,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newPages"] = ConvertFromPages(newPages),
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression = "SET #updateAt = :updateAt, #hash = :newHash, #data.#pages = list_append(#data.#pages, :newPages)",
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

        public async Task RemovePagesAsync(Guid ownerId, Guid scoreId, List<long> pageIds)
        {

            if (pageIds.Count == 0)
                throw new ArgumentException(nameof(pageIds));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.Page ??= new List<DynamoDbScorePageV1>();

            var existedIdSet = new HashSet<long>();
            pageIds.ForEach(id => existedIdSet.Add(id));

            var removeIndices = data.Page.Select((x, index) => (x, index))
                .Where(x => x.x != null && existedIdSet.Contains(x.x.Id))
                .Select(x => x.index)
                .ToArray();

            foreach (var index in removeIndices.Reverse())
            {
                data.Page.RemoveAt(index);
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, removeIndices, newHash, oldHash, now);

            static async Task<(DynamoDbScoreDataV1 data, string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result, hash);
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                int[] removeIndices,
                string newHash,
                string oldHash,
                DateTimeOffset now
            )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#pages"] = DynamoDbScorePropertyNames.DataPropertyNames.Pages,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression =
                        $"SET #updateAt = :updateAt, #hash = :newHash REMOVE {string.Join(", ", removeIndices.Select(i=>$"#data.#pages[{i}]"))}",
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

        public async Task ReplacePagesAsync(Guid ownerId, Guid scoreId, List<PatchScorePage> pages)
        {

            if (pages.Count == 0)
                throw new ArgumentException(nameof(pages));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.Page ??= new List<DynamoDbScorePageV1>();

            // Key id, Value index
            var pageIndices = new Dictionary<long,int>();
            foreach (var (databaseScoreDataPageV1,index) in data.Page.Select((x,index)=>(x,index)))
            {
                pageIndices[databaseScoreDataPageV1.Id] = index;
            }

            var replacingPages = new List<(DynamoDbScorePageV1 data, int targetIndex)>();

            foreach (var page in pages)
            {
                var id = page.TargetPageId;
                if(!pageIndices.TryGetValue(id, out var index))
                    throw new InvalidOperationException();

                var p = new DynamoDbScorePageV1()
                {
                    Id = id,
                    ItemId = ScoreDatabaseUtils.ConvertToBase64(page.ItemId),
                    Page = page.Page,
                };
                replacingPages.Add((p, index));
                data.Page[index] = p;
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, replacingPages, newHash, oldHash, now);

            static async Task<(DynamoDbScoreDataV1 data, string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result, hash);
            }


            static AttributeValue ConvertFromPage(DynamoDbScorePageV1 page)
            {
                var p = new Dictionary<string, AttributeValue>()
                {
                    [DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Id] = new AttributeValue()
                    {
                        N = page.Id.ToString(),
                    }
                };
                if (page.Page != null)
                {
                    p[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Page] = new AttributeValue(page.Page);
                }
                if (page.ItemId != null)
                {
                    p[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.ItemId] = new AttributeValue(page.ItemId);
                }
                if(p.Count == 0)
                    return null;

                return new AttributeValue() {M = p};
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                List<(DynamoDbScorePageV1 data, int targetIndex)> replacingPages,
                string newHash,
                string oldHash,
                DateTimeOffset now
            )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var replacingValues = replacingPages
                    .Select(x => (key: ":newPage" + x.targetIndex, value: ConvertFromPage(x.data), x.targetIndex))
                    .ToArray();
                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#pages"] = DynamoDbScorePropertyNames.DataPropertyNames.Pages,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>(replacingValues.ToDictionary(x=>x.key, x=>x.value))
                    {
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression =
                        $"SET #updateAt = :updateAt, #hash = :newHash, {string.Join(", ", replacingValues.Select((x)=>$"#data.#pages[{x.targetIndex}] = {x.key}"))}",
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

        public async Task AddAnnotationsAsync(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
                throw new ArgumentException(nameof(annotations));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.Annotations ??= new List<DynamoDbScoreAnnotationV1>();

            var newAnnotations = new List<DynamoDbScoreAnnotationV1>();

            var annotationId = data.Annotations.Count == 0 ? 0 : data.Annotations.Select(x => x.Id).Max() + 1;

            var newAnnotationContentHashDic = new Dictionary<string, NewScoreAnnotation>();
            var existedContentHashSet = new HashSet<string>();
            data.Annotations.ForEach(h => existedContentHashSet.Add(h.ContentHash));

            foreach (var annotation in annotations)
            {
                var hash = ScoreDatabaseUtils.CalcContentHash(annotation.Content);

                if(!existedContentHashSet.Contains(hash))
                    newAnnotationContentHashDic[hash] = annotation;

                var a = new DynamoDbScoreAnnotationV1()
                {
                    Id = annotationId++,
                    ContentHash = hash,
                };
                newAnnotations.Add(a);
                data.Annotations.Add(a);
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            var annotationCountMax = _quota.AnnotationCountMax;

            await AddAnnListAsync(_dynamoDbClient, ScoreDataTableName, owner, score, newAnnotationContentHashDic);
            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, newAnnotations, newHash, oldHash, now,annotationCountMax);

            static async Task AddAnnListAsync(
                IAmazonDynamoDB client, string tableName, string owner, string score,
                Dictionary<string, NewScoreAnnotation> newAnnotations)
            {
                const int chunkSize = 25;

                var chunkList = newAnnotations.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x => x.Select(y => (hash: y.x.Key, ann: y.x.Value)).ToArray())
                    .ToArray();

                foreach (var valueTuples in chunkList)
                {
                    await AddAnnList25Async(client, tableName, owner, score, valueTuples);
                }

                static async Task AddAnnList25Async(
                    IAmazonDynamoDB client, string tableName, string owner, string score,
                    (string hash, NewScoreAnnotation ann)[] annotations)
                {
                    Dictionary<string,List<WriteRequest>> request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = annotations.Select(a=>new WriteRequest()
                        {
                            PutRequest = new PutRequest()
                            {
                                Item = new Dictionary<string, AttributeValue>()
                                {
                                    [ScoreDataDatabasePropertyNames.OwnerId] = new AttributeValue(owner + score),
                                    [ScoreDataDatabasePropertyNames.DataId] = new AttributeValue(a.hash),
                                    [ScoreDataDatabasePropertyNames.Type] = new AttributeValue(ScoreDataDatabaseConstant.TypeAnnotation),
                                    [ScoreDataDatabasePropertyNames.Content] = new AttributeValue(a.ann.Content),
                                }
                            }
                        }).ToList(),
                    };
                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 追加に失敗したときにリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }

                }
            }

            static async Task<(DynamoDbScoreDataV1 data,string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);

                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result,hash);
            }

            static AttributeValue ConvertFromAnnotations(List<DynamoDbScoreAnnotationV1> annotations)
            {
                var result = new AttributeValue()
                {
                    L = new List<AttributeValue>()
                };

                foreach (var annotation in annotations)
                {
                    var a = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.Id] = new AttributeValue()
                        {
                            N = annotation.Id.ToString(),
                        }
                    };
                    if (annotation.ContentHash != null)
                    {
                        a[DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.ContentHash] = new AttributeValue(annotation.ContentHash);
                    }
                    if(a.Count == 0)
                        continue;

                    result.L.Add(new AttributeValue() {M = a});
                }

                return result;
            }
            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                List<DynamoDbScoreAnnotationV1> newAnnotations,
                string newHash,
                string oldHash,
                DateTimeOffset now,
                int annotationCountMax
                )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#annotations"] = DynamoDbScorePropertyNames.DataPropertyNames.Annotations,
                        ["#a_count"] = DynamoDbScorePropertyNames.DataPropertyNames.AnnotationCount,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newAnnotations"] = ConvertFromAnnotations(newAnnotations),
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                        [":annCountMax"] = new AttributeValue(){N = (annotationCountMax - newAnnotations.Count).ToString()},
                        [":addAnnCount"] = new AttributeValue(){N = newAnnotations.Count.ToString()},
                    },
                    ConditionExpression = "#hash = :oldHash and #data.#a_count < :annCountMax",
                    UpdateExpression = "SET #updateAt = :updateAt, #hash = :newHash, #data.#annotations = list_append(#data.#annotations, :newAnnotations) ADD #data.#a_count :addAnnCount",
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

        public async Task RemoveAnnotationsAsync(Guid ownerId, Guid scoreId, List<long> annotationIds)
        {

            if (annotationIds.Count == 0)
                throw new ArgumentException(nameof(annotationIds));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.Annotations ??= new List<DynamoDbScoreAnnotationV1>();

            var existedIdSet = new HashSet<long>();
            annotationIds.ForEach(id => existedIdSet.Add(id));

            var removeIndices = data.Annotations.Select((x, index) => (x, index))
                .Where(x => x.x != null && existedIdSet.Contains(x.x.Id))
                .Select(x => x.index)
                .ToArray();

            var removeHashSet = new HashSet<string>();
            foreach (var index in removeIndices.Reverse())
            {
                removeHashSet.Add(data.Annotations[index].ContentHash);
                data.Annotations.RemoveAt(index);
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, removeIndices, newHash, oldHash, now);

            await RemoveDataAsync(_dynamoDbClient, ScoreDataTableName, owner, score, removeHashSet);

            static async Task<(DynamoDbScoreDataV1 data, string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result, hash);
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                int[] removeIndices,
                string newHash,
                string oldHash,
                DateTimeOffset now
            )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#ann"] = DynamoDbScorePropertyNames.DataPropertyNames.Annotations,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression =
                        $"SET #updateAt = :updateAt, #hash = :newHash REMOVE {string.Join(", ", removeIndices.Select(i=>$"#data.#ann[{i}]"))}",
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

            static async Task RemoveDataAsync(
                IAmazonDynamoDB client, string tableName, string owner, string score,
                HashSet<string> removeHashSet)
            {
                const int chunkSize = 25;

                var chunkList = removeHashSet.Select((h, index) => (h, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x => x.Select(x => x.h).ToArray())
                    .ToArray();

                foreach (var hashList in chunkList)
                {
                    await RemoveData25Async(client, tableName, owner, score, hashList);
                }

                static async Task RemoveData25Async(
                    IAmazonDynamoDB client, string tableName, string owner, string score,
                    string[] hashList)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = hashList.Select(h=>new WriteRequest()
                        {
                            DeleteRequest = new DeleteRequest()
                            {
                                Key = new Dictionary<string, AttributeValue>()
                                {
                                    [ScoreDataDatabasePropertyNames.OwnerId] = new AttributeValue(owner + score),
                                    [ScoreDataDatabasePropertyNames.DataId] = new AttributeValue(h),
                                },
                            },
                        }).ToList(),
                    };

                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 削除時に失敗したデータを取得しリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            }
        }

        public async Task ReplaceAnnotationsAsync(Guid ownerId, Guid scoreId, List<PatchScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
                throw new ArgumentException(nameof(annotations));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            data.Annotations ??= new List<DynamoDbScoreAnnotationV1>();

            // Key id, Value index
            var annotationIndices = new Dictionary<long,int>();
            foreach (var (ann,index) in data.Annotations.Select((x,index)=>(x,index)))
            {
                annotationIndices[ann.Id] = index;
            }

            var replacingAnnotations = new List<(DynamoDbScoreAnnotationV1 data, int targetIndex)>();

            var existedAnnData = new HashSet<string>();
            data.Annotations.ForEach(x => existedAnnData.Add(x.ContentHash));
            var addAnnData = new Dictionary<string, PatchScoreAnnotation>();

            foreach (var ann in annotations)
            {
                var id = ann.TargetAnnotationId;
                if(!annotationIndices.TryGetValue(id, out var index))
                    throw new InvalidOperationException();

                var hash = ScoreDatabaseUtils.CalcContentHash(ann.Content);

                if (!existedAnnData.Contains(hash))
                {
                    addAnnData[hash] = ann;
                }

                var a = new DynamoDbScoreAnnotationV1()
                {
                    Id = id,
                    ContentHash = hash,
                };
                replacingAnnotations.Add((a, index));
                data.Annotations[index] = a;
            }

            var removeAnnData = existedAnnData.ToHashSet();

            foreach (var annotation in data.Annotations)
            {
                if (removeAnnData.Contains(annotation.ContentHash))
                    removeAnnData.Remove(annotation.ContentHash);
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, replacingAnnotations, newHash, oldHash, now);

            await AddAnnListAsync(_dynamoDbClient, ScoreDataTableName, owner, score, addAnnData);
            await RemoveAnnListAsync(_dynamoDbClient, ScoreDataTableName, owner, score, removeAnnData);

            static async Task<(DynamoDbScoreDataV1 data, string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);

                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result, hash);
            }


            static AttributeValue ConvertFromAnnotation(DynamoDbScoreAnnotationV1 annotation)
            {
                var a = new Dictionary<string, AttributeValue>()
                {
                    [DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.Id] = new AttributeValue()
                    {
                        N = annotation.Id.ToString(),
                    }
                };
                if (annotation.ContentHash != null)
                {
                    a[DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.ContentHash] = new AttributeValue(annotation.ContentHash);
                }
                if(a.Count == 0)
                    return null;

                return new AttributeValue() {M = a};
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                List<(DynamoDbScoreAnnotationV1 data, int targetIndex)> replacingAnnotations,
                string newHash,
                string oldHash,
                DateTimeOffset now
            )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var replacingValues = replacingAnnotations
                    .Select(x => (key: ":newAnn" + x.targetIndex, value: ConvertFromAnnotation(x.data), x.targetIndex))
                    .ToArray();
                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#hash"] = DynamoDbScorePropertyNames.DataHash,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#ann"] = DynamoDbScorePropertyNames.DataPropertyNames.Annotations,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>(replacingValues.ToDictionary(x=>x.key, x=>x.value))
                    {
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression =
                        $"SET #updateAt = :updateAt, #hash = :newHash, {string.Join(", ", replacingValues.Select((x)=>$"#data.#ann[{x.targetIndex}] = {x.key}"))}",
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


            static async Task AddAnnListAsync(
                IAmazonDynamoDB client, string tableName, string owner, string score,
                Dictionary<string, PatchScoreAnnotation> newAnnotations)
            {
                const int chunkSize = 25;

                var chunkList = newAnnotations.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x => x.Select(y => (hash: y.x.Key, ann: y.x.Value)).ToArray())
                    .ToArray();

                foreach (var valueTuples in chunkList)
                {
                    await AddAnnList25Async(client, tableName, owner, score, valueTuples);
                }

                static async Task AddAnnList25Async(
                    IAmazonDynamoDB client, string tableName, string owner, string score,
                    (string hash, PatchScoreAnnotation ann)[] annotations)
                {
                    Dictionary<string,List<WriteRequest>> request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = annotations.Select(a=>new WriteRequest()
                        {
                            PutRequest = new PutRequest()
                            {
                                Item = new Dictionary<string, AttributeValue>()
                                {
                                    [ScoreDataDatabasePropertyNames.OwnerId] = new AttributeValue(owner + score),
                                    [ScoreDataDatabasePropertyNames.DataId] = new AttributeValue(a.hash),
                                    [ScoreDataDatabasePropertyNames.Type] = new AttributeValue(ScoreDataDatabaseConstant.TypeAnnotation),
                                    [ScoreDataDatabasePropertyNames.Content] = new AttributeValue(a.ann.Content),
                                }
                            }
                        }).ToList(),
                    };
                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 追加に失敗したときにリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }

                }
            }


            static async Task RemoveAnnListAsync(
                IAmazonDynamoDB client, string tableName, string owner, string score,
                HashSet<string> removeAnnotations)
            {
                const int chunkSize = 25;

                var chunkList = removeAnnotations.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x => x.Select(y => y.x).ToArray())
                    .ToArray();

                foreach (var hashList in chunkList)
                {
                    await RemoveAnnList25Async(client, tableName, owner, score, hashList);
                }

                static async Task RemoveAnnList25Async(
                    IAmazonDynamoDB client, string tableName, string owner, string score,
                    string[] annotations)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = annotations.Select(hash=>new WriteRequest()
                        {
                            DeleteRequest = new DeleteRequest()
                            {
                                Key = new Dictionary<string, AttributeValue>()
                                {
                                    [ScoreDataDatabasePropertyNames.OwnerId] = new AttributeValue(owner + score),
                                    [ScoreDataDatabasePropertyNames.DataId] = new AttributeValue(hash),
                                }
                            }
                        }).ToList(),
                    };
                    try
                    {
                        await client.BatchWriteItemAsync(request);
                    }
                    catch (Exception ex)
                    {
                        // TODO 追加に失敗したときにリトライ処理を入れる
                        Console.WriteLine(ex.Message);
                        throw;
                    }

                }
            }
        }

        public async Task<IReadOnlyList<ScoreSummary>> GetScoreSummariesAsync(Guid ownerId)
        {

            return await GetAsync(_dynamoDbClient, ScoreTableName, ownerId);


            static async Task<ScoreSummary[]> GetAsync(IAmazonDynamoDB client, string tableName, Guid ownerId)
            {
                var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId,
                        ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                        ["#data"] = DynamoDbScorePropertyNames.Data,
                        ["#title"] = DynamoDbScorePropertyNames.DataPropertyNames.Title,
                        ["#desc"] = DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner),
                        [":mainPrefix"] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#score, :mainPrefix)",
                    ProjectionExpression = "#owner, #score, #data.#title, #data.#desc",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    var subStartIndex = ScoreDatabaseConstant.ScoreIdMainPrefix.Length;

                    return response.Items
                        .Select(x =>
                        {
                            var ownerId64 = x[DynamoDbScorePropertyNames.OwnerId].S;
                            var scoreId64 = x[DynamoDbScorePropertyNames.ScoreId].S.Substring(subStartIndex);
                            var title = x[DynamoDbScorePropertyNames.Data].M[DynamoDbScorePropertyNames.DataPropertyNames.Title].S;
                            var description = x[DynamoDbScorePropertyNames.Data].M[DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash].S;

                            var ownerId = ScoreDatabaseUtils.ConvertToGuid(ownerId64);
                            var scoreId = ScoreDatabaseUtils.ConvertToGuid(scoreId64);

                            return new ScoreSummary()
                            {
                                Id = scoreId,
                                OwnerId = ownerId,
                                Title = title,
                                Description = description
                            };
                        })
                        .ToArray();
                }
                catch (InternalServerErrorException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ProvisionedThroughputExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (RequestLimitExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ResourceNotFoundException ex)
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

        public async Task<(DynamoDbScore score, Dictionary<string, string> hashSet)> GetDynamoDbScoreDataAsync(
            Guid ownerId, Guid scoreId)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var dynamoDbScore = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);
            var hashSet = await GetAnnotationsAsync(_dynamoDbClient, ScoreDataTableName, owner, score);

            return (dynamoDbScore, hashSet);

            static async Task<DynamoDbScore> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);

                var dynamoDbScore = new DynamoDbScore(response.Item);

                return dynamoDbScore;
            }


            static async Task<Dictionary<string, string>> GetAnnotationsAsync(
                IAmazonDynamoDB client, string tableName,
                string owner, string score)
            {
                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreDataDatabasePropertyNames.OwnerId,
                        ["#data"] = ScoreDataDatabasePropertyNames.DataId,
                        ["#content"] = ScoreDataDatabasePropertyNames.Content,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner + score),
                    },
                    KeyConditionExpression = "#owner = :owner",
                    ProjectionExpression = "#data, #content",
                };

                try
                {
                    var response = await client.QueryAsync(request);

                    var result = new Dictionary<string, string>();
                    foreach (var item in response.Items)
                    {
                        var hashValue = item[ScoreDataDatabasePropertyNames.DataId];
                        var contentValue = item[ScoreDataDatabasePropertyNames.Content];
                        result[hashValue.S] = contentValue.S;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

            }
        }

        public async Task<DatabaseScoreRecord> GetSnapshotScoreDetailAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
            var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

            var record = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score, snapshot);

            return record;

            static async Task<DatabaseScoreRecord> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string snapshot)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshot),
                    },
                };
                var response = await client.GetItemAsync(request);

                if(!response.Item.TryGetValue(DynamoDbScorePropertyNames.Data, out var data))
                    throw new NotFoundSnapshotException("Not found snapshot.");

                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;

                var createAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(response.Item[DynamoDbScorePropertyNames.CreateAt].S);
                var updateAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(response.Item[DynamoDbScorePropertyNames.UpdateAt].S);

                var snapshotName = response.Item[DynamoDbScorePropertyNames.SnapshotName].S;
                return new DatabaseScoreSnapshotRecord()
                {
                    CreateAt = createAt,
                    UpdateAt = updateAt,
                    DataHash = hash,
                    Data = result,
                    SnapshotName = snapshotName,
                };
            }
        }

        public async Task<(ScoreSnapshotDetail snapshot, ScoreAccesses access)> CreateSnapshotAsync(Guid ownerId,
            Guid scoreId,
            string snapshotName)
        {
            // TODO ここで作成されたデータを使い JSON ファイルを作成し S3 に保存する

            var snapshotId = Guid.NewGuid();
            var response = await CreateSnapshotAsync(ownerId, scoreId, snapshotId, snapshotName);
            var snapshot = new ScoreSnapshotDetail()
            {
                Id = snapshotId,
                Data = ScoreData.Create(response.data,response.annotations),
                Name = snapshotName,
            };

            return (snapshot, response.access);
        }

        public async Task<(DynamoDbScoreDataV1 data, Dictionary<string, string> annotations, ScoreAccesses access)> CreateSnapshotAsync(
            Guid ownerId, Guid scoreId, Guid snapshotId, string snapshotName)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
            var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

            var (dataValue,hash,access) = await GetAsync(_dynamoDbClient, ScoreTableName, owner, score);

            DynamoDbScoreDataV1.TryMapFromAttributeValue(dataValue, out var scoreData);
            var annotations = await GetAnnotationsAsync(_dynamoDbClient, ScoreDataTableName, owner, score, snapshot);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            var maxSnapshotCount = _quota.SnapshotCountMax;
            await UpdateAsync(
                _dynamoDbClient, ScoreTableName, owner, score, snapshot
                , snapshotName, now, maxSnapshotCount);

            return (scoreData, annotations, access);

            static async Task<(AttributeValue data,string hash, ScoreAccesses access)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new NotFoundScoreException("Not found score.");

                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;

                var accessText = response.Item[DynamoDbScorePropertyNames.Access].S;

                var access = ScoreDatabaseConstant.ScoreAccessPublic == accessText
                    ? ScoreAccesses.Public
                    : ScoreAccesses.Private;

                return (data, hash, access);
            }

            static async Task<Dictionary<string, string>> GetAnnotationsAsync(
                IAmazonDynamoDB client, string tableName, string owner, string score, string snapshot)
            {
                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreDataDatabasePropertyNames.OwnerId,
                        ["#data"] = ScoreDataDatabasePropertyNames.DataId,
                        ["#content"] = ScoreDataDatabasePropertyNames.Content,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner + score),
                    },
                    KeyConditionExpression = "#owner = :owner",
                    ProjectionExpression = "#data, #content",
                };

                try
                {
                    var response = await client.QueryAsync(request);

                    var result = new Dictionary<string, string>();
                    foreach (var item in response.Items)
                    {
                        var hashValue = item[ScoreDataDatabasePropertyNames.DataId];
                        var contentValue = item[ScoreDataDatabasePropertyNames.Content];
                        result[hashValue.S] = contentValue.S;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string snapshot,
                string snapshotName,
                DateTimeOffset now,
                int maxSnapshotCount
                )
            {
                var at = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#snapshotCount"] = DynamoDbScorePropertyNames.SnapshotCount,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "1"},
                                [":countMax"] = new AttributeValue()
                                {
                                    N = maxSnapshotCount.ToString(),
                                },
                            },
                            ConditionExpression = "#snapshotCount < :countMax",
                            UpdateExpression = "ADD #snapshotCount :increment"
                        }
                    },
                    new TransactWriteItem()
                    {
                        Put = new Put()
                        {
                            TableName = tableName,
                            Item = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshot),
                                [DynamoDbScorePropertyNames.CreateAt] = new AttributeValue(at),
                                [DynamoDbScorePropertyNames.UpdateAt] = new AttributeValue(at),
                                [DynamoDbScorePropertyNames.SnapshotName] = new AttributeValue(snapshotName),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                            },
                            ConditionExpression = "attribute_not_exists(#score)",
                        }
                    }
                };

                try
                {
                    await client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                    {
                        TransactItems = actions,
                        ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL
                    });
                }
                catch (TransactionCanceledException ex)
                {
                    var updateReason = ex.CancellationReasons[0];

                    if (updateReason.Code == "ConditionalCheckFailed")
                    {
                        throw new CreatedSnapshotException(CreatedSnapshotExceptionCodes.ExceededUpperLimit, ex);
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

        public async Task DeleteSnapshotAsync(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);
            var snapshot = ScoreDatabaseUtils.ConvertToBase64(snapshotId);

            await DeleteItemAsync(_dynamoDbClient, ScoreTableName, owner, score, snapshot);

            static async Task DeleteItemAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string snapshot
                )
            {
                var actions = new List<TransactWriteItem>()
                {
                    new TransactWriteItem()
                    {
                        Delete = new Delete()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshot),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                            },
                            ConditionExpression = "attribute_exists(#score)",
                        },
                    },
                    new TransactWriteItem()
                    {
                        Update = new Update()
                        {
                            TableName = tableName,
                            Key = new Dictionary<string, AttributeValue>()
                            {
                                [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                                [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#snapshotCount"] = DynamoDbScorePropertyNames.SnapshotCount,
                            },
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                            {
                                [":increment"] = new AttributeValue(){N = "-1"},
                            },
                            UpdateExpression = "ADD #snapshotCount :increment",
                        }
                    },
                };

                try
                {
                    await client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
                    {
                        TransactItems = actions,
                        ReturnConsumedCapacity = ReturnConsumedCapacity.TOTAL,
                    });
                }
                catch (TransactionCanceledException ex)
                {
                    var deleteReason = ex.CancellationReasons[0];

                    if (deleteReason.Code == "ConditionalCheckFailed")
                    {
                        throw new NotFoundSnapshotException(ex);
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

        public async Task<ScoreSnapshotSummary[]> GetSnapshotSummariesAsync(Guid ownerId,
            Guid scoreId)
        {

            return await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            static async Task<ScoreSnapshotSummary[]> GetAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = DynamoDbScorePropertyNames.OwnerId,
                        ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                        ["#snapshotName"] = DynamoDbScorePropertyNames.SnapshotName,
                        ["#createAt"] = DynamoDbScorePropertyNames.CreateAt,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":owner"] = new AttributeValue(owner),
                        [":snapPrefix"] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score),
                    },
                    KeyConditionExpression = "#owner = :owner and begins_with(#score, :snapPrefix)",
                    ProjectionExpression = "#score, #snapshotName, #createAt",
                };
                try
                {
                    var response = await client.QueryAsync(request);

                    var subStartIndex = (ScoreDatabaseConstant.ScoreIdSnapPrefix + score).Length;

                    return response.Items
                        .Select(x =>(
                                score: x[DynamoDbScorePropertyNames.ScoreId].S,
                                name: x[DynamoDbScorePropertyNames.SnapshotName].S,
                                createAt: x[DynamoDbScorePropertyNames.CreateAt].S)
                        )
                        .Select(x =>
                            new ScoreSnapshotSummary()
                            {
                                Id = ScoreDatabaseUtils.ConvertToGuid(x.score.Substring(subStartIndex)),
                                Name =x.name,
                                CreateAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(x.createAt),
                            }
                        )
                        .ToArray();
                }
                catch (InternalServerErrorException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ProvisionedThroughputExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (RequestLimitExceededException ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                catch (ResourceNotFoundException ex)
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

        public async Task SetAccessAsync(Guid ownerId, Guid scoreId, ScoreAccesses access)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, owner, score, access, now);

            static async Task<(DynamoDbScoreDataV1 data,string hash)> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score)
            {
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[DynamoDbScorePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                DynamoDbScoreDataV1.TryMapFromAttributeValue(data, out var result);
                var hash = response.Item[DynamoDbScorePropertyNames.DataHash].S;
                return (result,hash);
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                ScoreAccesses access,
                DateTimeOffset now
                )
            {
                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);
                var accessText = ScoreDatabaseUtils.ConvertFromScoreAccess(access);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(owner),
                        [DynamoDbScorePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#score"] = DynamoDbScorePropertyNames.ScoreId,
                        ["#updateAt"] = DynamoDbScorePropertyNames.UpdateAt,
                        ["#access"] = DynamoDbScorePropertyNames.Access,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":access"] = new AttributeValue(accessText),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "attribute_exists(#score)",
                    UpdateExpression = "SET #updateAt = :updateAt, #access = :access",
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
}
