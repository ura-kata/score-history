using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
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
        public string TableName { get; } = "ura-kata-score-history";

        public ScoreDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient)
        {
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }
        public ScoreDatabase(IScoreQuota quota, IAmazonDynamoDB dynamoDbClient,string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException(nameof(tableName));

            TableName = tableName;
            _quota = quota;
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task InitializeAsync(Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                throw new ArgumentException(nameof(ownerId));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

            await PutAsync(_dynamoDbClient, TableName, owner);

            static async Task PutAsync(IAmazonDynamoDB client, string tableName, string owner)
            {
                var request = new PutItemRequest()
                {
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                        [ScoreDatabasePropertyNames.ScoreCount] = new AttributeValue(){N = "0"}
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

        public async Task CreateAsync(Guid ownerId, string title, string description)
        {
            var newScoreId = Guid.NewGuid();
            await CreateAsync(ownerId, newScoreId, title, description);
        }
        public async Task CreateAsync(Guid ownerId, Guid newScoreId, string title, string description)
        {
            if (ownerId == Guid.Empty)
                throw new ArgumentException(nameof(ownerId));
            if (newScoreId == Guid.Empty)
                throw new ArgumentException(nameof(newScoreId));
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var preprocessingTitle = title.Trim();
            if (preprocessingTitle == "")
                throw new ArgumentException(nameof(title));

            var scoreCountMax = _quota.ScoreCountMax;

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await PutScoreAsync(
                _dynamoDbClient, TableName, ownerId, newScoreId, scoreCountMax,
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

                var data = new DatabaseScoreDataV1()
                {
                    Title = title,
                    Description = description,
                };
                var dataAttributeValue = ScoreDatabaseUtils.ConvertToDatabaseDataV1(data);
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
                                [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                                [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#count"] = ScoreDatabasePropertyNames.ScoreCount,
                                ["#scores"] = ScoreDatabasePropertyNames.Scores,
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
                                [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                                [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + newScore),
                                [ScoreDatabasePropertyNames.DataHash] = new AttributeValue(dataHash),
                                [ScoreDatabasePropertyNames.CreateAt] = new AttributeValue(createAt),
                                [ScoreDatabasePropertyNames.UpdateAt] = new AttributeValue(updateAt),
                                [ScoreDatabasePropertyNames.Data] = dataAttributeValue,
                            },
                            TableName = tableName,
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = ScoreDatabasePropertyNames.ScoreId,
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

        public async Task DeleteAsync(Guid ownerId, Guid scoreId)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            await DeleteMainAsync(_dynamoDbClient, TableName, owner, score);

            // スナップショットの削除は SQS を使ったほうがいいかも
            var snapshotScoreIds = await GetSnapshotScoreIdsAsync(_dynamoDbClient, TableName, owner, score);

            await DeleteSnapshotsAsync(_dynamoDbClient, TableName, owner, snapshotScoreIds);

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
                                [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                                [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#score"] = ScoreDatabasePropertyNames.ScoreId,
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
                                [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                                [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSummary),
                            },
                            ExpressionAttributeNames = new Dictionary<string, string>()
                            {
                                ["#count"] = ScoreDatabasePropertyNames.ScoreCount,
                                ["#scores"] = ScoreDatabasePropertyNames.Scores,
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
                        ["#owner"] = ScoreDatabasePropertyNames.OwnerId,
                        ["#score"] = ScoreDatabasePropertyNames.ScoreId,
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

                    return response.Items.Select(x => x[ScoreDatabasePropertyNames.ScoreId]?.S)
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
                                    [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                                    [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(scoreId),
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

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Title = title;

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();
            await UpdateAsync(_dynamoDbClient, TableName, owner, score, title, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data,string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#title"] = ScoreDatabasePropertyNames.Title,
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

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Description = description;

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();
            await UpdateAsync(_dynamoDbClient, TableName, owner, score, description, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data,string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#desc"] = ScoreDatabasePropertyNames.Description,
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

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Page ??= new List<DatabaseScoreDataPageV1>();

            var newPages = new List<DatabaseScoreDataPageV1>();

            var pageId = data.Page.Count == 0 ? 0 : data.Page.Select(x => x.Id).Max() + 1;
            foreach (var page in pages)
            {
                var p = new DatabaseScoreDataPageV1()
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
            await UpdateAsync(_dynamoDbClient, TableName, owner, score, newPages, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data,string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
                return (result,hash);
            }

            static AttributeValue ConvertFromPages(List<DatabaseScoreDataPageV1> pages)
            {
                var result = new AttributeValue()
                {
                    L = new List<AttributeValue>()
                };

                foreach (var page in pages)
                {
                    var p = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreDatabasePropertyNames.PagesId] = new AttributeValue()
                        {
                            N = page.Id.ToString(),
                        }
                    };
                    if (page.Page != null)
                    {
                        p[ScoreDatabasePropertyNames.PagesPage] = new AttributeValue(page.Page);
                    }
                    if (page.ItemId != null)
                    {
                        p[ScoreDatabasePropertyNames.PagesItemId] = new AttributeValue(page.ItemId);
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
                List<DatabaseScoreDataPageV1> newPages,
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#pages"] = ScoreDatabasePropertyNames.Pages,
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

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Page ??= new List<DatabaseScoreDataPageV1>();

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

            await UpdateAsync(_dynamoDbClient, TableName, owner, score, removeIndices, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data, string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#pages"] = ScoreDatabasePropertyNames.Pages,
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

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Page ??= new List<DatabaseScoreDataPageV1>();

            // Key id, Value index
            var pageIndices = new Dictionary<long,int>();
            foreach (var (databaseScoreDataPageV1,index) in data.Page.Select((x,index)=>(x,index)))
            {
                pageIndices[databaseScoreDataPageV1.Id] = index;
            }

            var replacingPages = new List<(DatabaseScoreDataPageV1 data, int targetIndex)>();

            foreach (var page in pages)
            {
                var id = page.TargetPageId;
                if(!pageIndices.TryGetValue(id, out var index))
                    throw new InvalidOperationException();

                var p = new DatabaseScoreDataPageV1()
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

            await UpdateAsync(_dynamoDbClient, TableName, owner, score, replacingPages, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data, string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
                return (result, hash);
            }


            static AttributeValue ConvertFromPage(DatabaseScoreDataPageV1 page)
            {
                var p = new Dictionary<string, AttributeValue>()
                {
                    [ScoreDatabasePropertyNames.PagesId] = new AttributeValue()
                    {
                        N = page.Id.ToString(),
                    }
                };
                if (page.Page != null)
                {
                    p[ScoreDatabasePropertyNames.PagesPage] = new AttributeValue(page.Page);
                }
                if (page.ItemId != null)
                {
                    p[ScoreDatabasePropertyNames.PagesItemId] = new AttributeValue(page.ItemId);
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
                List<(DatabaseScoreDataPageV1 data, int targetIndex)> replacingPages,
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#pages"] = ScoreDatabasePropertyNames.Pages,
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

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Annotations ??= new List<DatabaseScoreDataAnnotationV1>();

            var newAnnotations = new List<DatabaseScoreDataAnnotationV1>();

            var annotationId = data.Annotations.Count == 0 ? 0 : data.Annotations.Select(x => x.Id).Max() + 1;
            foreach (var annotation in annotations)
            {
                var a = new DatabaseScoreDataAnnotationV1()
                {
                    Id = annotationId++,
                    Content = annotation.Content,
                };
                newAnnotations.Add(a);
                data.Annotations.Add(a);
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            // TODO アノテーションの追加上限値判定を追加
            await UpdateAsync(_dynamoDbClient, TableName, owner, score, newAnnotations, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data,string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
                return (result,hash);
            }

            static AttributeValue ConvertFromAnnotations(List<DatabaseScoreDataAnnotationV1> annotations)
            {
                var result = new AttributeValue()
                {
                    L = new List<AttributeValue>()
                };

                foreach (var annotation in annotations)
                {
                    var a = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreDatabasePropertyNames.AnnotationsId] = new AttributeValue()
                        {
                            N = annotation.Id.ToString(),
                        }
                    };
                    if (annotation.Content != null)
                    {
                        a[ScoreDatabasePropertyNames.AnnotationsContent] = new AttributeValue(annotation.Content);
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
                List<DatabaseScoreDataAnnotationV1> newAnnotations,
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#annotations"] = ScoreDatabasePropertyNames.Annotations,
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                    {
                        [":newAnnotations"] = ConvertFromAnnotations(newAnnotations),
                        [":newHash"] = new AttributeValue(newHash),
                        [":oldHash"] = new AttributeValue(oldHash),
                        [":updateAt"] = new AttributeValue(updateAt),
                    },
                    ConditionExpression = "#hash = :oldHash",
                    UpdateExpression = "SET #updateAt = :updateAt, #hash = :newHash, #data.#annotations = list_append(#data.#annotations, :newAnnotations)",
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

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Annotations ??= new List<DatabaseScoreDataAnnotationV1>();

            var existedIdSet = new HashSet<long>();
            annotationIds.ForEach(id => existedIdSet.Add(id));

            var removeIndices = data.Annotations.Select((x, index) => (x, index))
                .Where(x => x.x != null && existedIdSet.Contains(x.x.Id))
                .Select(x => x.index)
                .ToArray();

            foreach (var index in removeIndices.Reverse())
            {
                data.Annotations.RemoveAt(index);
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, TableName, owner, score, removeIndices, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data, string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#ann"] = ScoreDatabasePropertyNames.Annotations,
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
        }

        public async Task ReplaceAnnotationsAsync(Guid ownerId, Guid scoreId, List<PatchScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
                throw new ArgumentException(nameof(annotations));

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var (data, oldHash) = await GetAsync(_dynamoDbClient, TableName, owner, score);

            data.Annotations ??= new List<DatabaseScoreDataAnnotationV1>();

            // Key id, Value index
            var annotationIndices = new Dictionary<long,int>();
            foreach (var (ann,index) in data.Annotations.Select((x,index)=>(x,index)))
            {
                annotationIndices[ann.Id] = index;
            }

            var replacingAnnotations = new List<(DatabaseScoreDataAnnotationV1 data, int targetIndex)>();

            foreach (var ann in annotations)
            {
                var id = ann.TargetAnnotationId;
                if(!annotationIndices.TryGetValue(id, out var index))
                    throw new InvalidOperationException();

                var a = new DatabaseScoreDataAnnotationV1()
                {
                    Id = id,
                    Content = ann.Content,
                };
                replacingAnnotations.Add((a, index));
                data.Annotations[index] = a;
            }

            var newHash = ScoreDatabaseUtils.CalcHash(data);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, TableName, owner, score, replacingAnnotations, newHash, oldHash, now);

            static async Task<(DatabaseScoreDataV1 data, string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
                return (result, hash);
            }


            static AttributeValue ConvertFromAnnotation(DatabaseScoreDataAnnotationV1 annotation)
            {
                var a = new Dictionary<string, AttributeValue>()
                {
                    [ScoreDatabasePropertyNames.AnnotationsId] = new AttributeValue()
                    {
                        N = annotation.Id.ToString(),
                    }
                };
                if (annotation.Content != null)
                {
                    a[ScoreDatabasePropertyNames.AnnotationsContent] = new AttributeValue(annotation.Content);
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
                List<(DatabaseScoreDataAnnotationV1 data, int targetIndex)> replacingAnnotations,
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#updateAt"] = ScoreDatabasePropertyNames.UpdateAt,
                        ["#hash"] = ScoreDatabasePropertyNames.DataHash,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#ann"] = ScoreDatabasePropertyNames.Annotations,
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
        }

        public async Task<IReadOnlyList<ScoreSummary>> GetScoreSummariesAsync(Guid ownerId)
        {

            return await GetAsync(_dynamoDbClient, TableName, ownerId);


            static async Task<ScoreSummary[]> GetAsync(IAmazonDynamoDB client, string tableName, Guid ownerId)
            {
                var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreDatabasePropertyNames.OwnerId,
                        ["#score"] = ScoreDatabasePropertyNames.ScoreId,
                        ["#data"] = ScoreDatabasePropertyNames.Data,
                        ["#title"] = ScoreDatabasePropertyNames.Title,
                        ["#desc"] = ScoreDatabasePropertyNames.Description,
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
                            var ownerId64 = x[ScoreDatabasePropertyNames.OwnerId].S;
                            var scoreId64 = x[ScoreDatabasePropertyNames.ScoreId].S.Substring(subStartIndex);
                            var title = x[ScoreDatabasePropertyNames.Data].M[ScoreDatabasePropertyNames.Title].S;
                            var description = x[ScoreDatabasePropertyNames.Data].M[ScoreDatabasePropertyNames.Description].S;

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

        public async Task<DatabaseScoreRecord> GetDatabaseScoreRecordAsync(Guid ownerId, Guid scoreId)
        {

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var record = await GetAsync(_dynamoDbClient, TableName, owner, score);

            return record;

            static async Task<DatabaseScoreRecord> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");


                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;

                var createAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(response.Item[ScoreDatabasePropertyNames.CreateAt].S);
                var updateAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(response.Item[ScoreDatabasePropertyNames.UpdateAt].S);
                return new DatabaseScoreRecord()
                {
                    CreateAt = createAt,
                    UpdateAt = updateAt,
                    DataHash = hash,
                    Data = result,
                };
            }
        }

        public async Task<DatabaseScoreRecord> GetSnapshotScoreDetailAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var record = await GetAsync(_dynamoDbClient, TableName, owner, score, snapshotName);

            return record;

            static async Task<DatabaseScoreRecord> GetAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string snapshotName)
            {
                var base64SnapshotName = ScoreDatabaseUtils.EncodeToBase64FromSnapshotName(snapshotName);
                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] =
                            new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + base64SnapshotName),
                    },
                };
                var response = await client.GetItemAsync(request);

                if(!response.Item.TryGetValue(ScoreDatabasePropertyNames.Data, out var data))
                    throw new InvalidOperationException("not found.");

                var result = ScoreDatabaseUtils.ConvertToDatabaseScoreDataV1(data);
                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;

                var createAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(response.Item[ScoreDatabasePropertyNames.CreateAt].S);
                var updateAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(response.Item[ScoreDatabasePropertyNames.UpdateAt].S);
                return new DatabaseScoreRecord()
                {
                    CreateAt = createAt,
                    UpdateAt = updateAt,
                    DataHash = hash,
                    Data = result,
                };
            }
        }

        public async Task CreateSnapshotAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {

            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            var value = await GetAsync(_dynamoDbClient, TableName, owner, score);

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            // TODO スナップショットの作成数上限値判定を追加
            await UpdateAsync(_dynamoDbClient, TableName, owner, score, snapshotName, value, now);

            static async Task<(AttributeValue data,string hash)> GetAsync(
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
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdMainPrefix + score),
                    },
                };
                var response = await client.GetItemAsync(request);
                var data = response.Item[ScoreDatabasePropertyNames.Data];

                if (data is null)
                    throw new InvalidOperationException("not found.");

                var hash = response.Item[ScoreDatabasePropertyNames.DataHash].S;
                return (data,hash);
            }

            static async Task UpdateAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string snapshotName,
                (AttributeValue data, string hash) value,
                DateTimeOffset now
                )
            {
                var at = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);
                var snapshotNameBase64 = ScoreDatabaseUtils.EncodeToBase64FromSnapshotName(snapshotName);

                var request = new PutItemRequest()
                {
                    TableName = tableName,
                    Item = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshotNameBase64),
                        [ScoreDatabasePropertyNames.CreateAt] = new AttributeValue(at),
                        [ScoreDatabasePropertyNames.UpdateAt] = new AttributeValue(at),
                        [ScoreDatabasePropertyNames.DataHash] = new AttributeValue(value.hash),
                        [ScoreDatabasePropertyNames.Data] = value.data,
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#score"] = ScoreDatabasePropertyNames.ScoreId,
                    },
                    ConditionExpression = "attribute_not_exists(#score)",
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

        public async Task DeleteSnapshotAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {
            var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

            await DeleteItemAsync(_dynamoDbClient, TableName, owner, score, snapshotName);

            static async Task DeleteItemAsync(
                IAmazonDynamoDB client,
                string tableName,
                string owner,
                string score,
                string snapshotName
                )
            {
                var snapshotNameBase64 = ScoreDatabaseUtils.EncodeToBase64FromSnapshotName(snapshotName);

                var request = new DeleteItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreDatabasePropertyNames.OwnerId] = new AttributeValue(owner),
                        [ScoreDatabasePropertyNames.ScoreId] = new AttributeValue(ScoreDatabaseConstant.ScoreIdSnapPrefix + score + snapshotNameBase64),
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#score"] = ScoreDatabasePropertyNames.ScoreId,
                    },
                    ConditionExpression = "attribute_exists(#score)",
                };
                try
                {
                    await client.DeleteItemAsync(request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }

        public async Task<IReadOnlyList<string>> GetSnapshotNamesAsync(Guid ownerId, Guid scoreId)
        {

            return await GetAsync(_dynamoDbClient, TableName, ownerId, scoreId);

            static async Task<string[]> GetAsync(IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId)
            {
                var owner = ScoreDatabaseUtils.ConvertToBase64(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new QueryRequest()
                {
                    TableName = tableName,
                    ExpressionAttributeNames = new Dictionary<string, string>()
                    {
                        ["#owner"] = ScoreDatabasePropertyNames.OwnerId,
                        ["#score"] = ScoreDatabasePropertyNames.ScoreId,
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

                    var subStartIndex = (ScoreDatabaseConstant.ScoreIdSnapPrefix + score).Length;

                    return response.Items.Select(x => x[ScoreDatabasePropertyNames.ScoreId]?.S)
                        .Where(x => !(x is null))
                        .Select(x => x.Substring(subStartIndex))
                        .Select(ScoreDatabaseUtils.DecodeToSnapshotNameFromBase64)
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
    }
}
