using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics
{
    public static class ScoreDatabasePropertyNames
    {
        public const string OwnerId = "owner";
        public const string ScoreId = "score";
        public const string DataHash = "d_hash";
        public const string CreateAt = "create_at";
        public const string UpdateAt = "update_at";
        public const string Data = "data";
        public const string Title = "title";
        public const string Description = "desc";
        public const string DataVersion = "v";
        public const string Pages = "page";
        public const string PagesItemId = "item";
        public const string PagesPage = "page";
        public const string Annotations = "anno";
        public const string ScoreCount = "score_count";
        public const string Scores = "scores";
    }

    public static class ScoreDatabaseConstant
    {
        public const string ScoreIdMainPrefix = "main:";
        public const string ScoreIdSnapPrefix = "snap:";
        public const string ScoreIdSummary = "summary";
        public const string ScoreDataVersion1 = "1";
    }


    public class DatabaseScoreDataPageV1
    {
        [JsonPropertyName(ScoreDatabasePropertyNames.PagesItemId)]
        public string ItemId { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.PagesPage)]
        public string Page { get; set; }
    }
    public class DatabaseScoreDataV1
    {
        [JsonPropertyName(ScoreDatabasePropertyNames.Title)]
        public string Title { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.Description)]
        public string Description { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.DataVersion)]
        public string Version { get; set; } = ScoreDatabaseConstant.ScoreDataVersion1;
        [JsonPropertyName(ScoreDatabasePropertyNames.Pages)]
        public Dictionary<string, DatabaseScoreDataPageV1> Page { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.Annotations)]
        public Dictionary<string, string> Annotations { get; set; }
    }
    public static class ScoreDatabaseUtils
    {
        public static string ConvertToBase64(Guid id) =>
            Convert.ToBase64String(id.ToByteArray());

        public static Guid ConvertToGuid(string id) =>
            new Guid(Convert.FromBase64String(id));

        public static string CalcHash(DatabaseScoreDataV1 data)
        {
            var option = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.Default,
            };
            var json = JsonSerializer.SerializeToUtf8Bytes(data, option);
            return Convert.ToBase64String(MD5.Create().ComputeHash(json));
        }

        public static AttributeValue ConvertToDatabaseDataV1(DatabaseScoreDataV1 data)
        {
            var databaseData = new Dictionary<string, AttributeValue>();

            if (!string.IsNullOrWhiteSpace(data.Title))
            {
                databaseData[ScoreDatabasePropertyNames.Title] = new AttributeValue(data.Title);
            }
            if (!string.IsNullOrWhiteSpace(data.Description))
            {
                databaseData[ScoreDatabasePropertyNames.Description] = new AttributeValue(data.Description);
            }
            if (!string.IsNullOrWhiteSpace(data.Version))
            {
                databaseData[ScoreDatabasePropertyNames.DataVersion] = new AttributeValue(data.Version);
            }
            if (0 < data.Page?.Count)
            {
                var pages = new Dictionary<string, AttributeValue>();

                foreach (var (key,value) in data.Page)
                {
                    var page = new Dictionary<string, AttributeValue>();

                    if (value.Page != null)
                    {
                        page[ScoreDatabasePropertyNames.PagesPage] = new AttributeValue(value.Page);
                    }
                    if (value.ItemId != null)
                    {
                        page[ScoreDatabasePropertyNames.PagesItemId] = new AttributeValue(value.ItemId);
                    }

                    if(page.Count == 0)
                        continue;
                    pages[key] = new AttributeValue() {M = page};
                }

                if (0 < pages.Count)
                {
                    databaseData[ScoreDatabasePropertyNames.Pages] = new AttributeValue() {M = pages};
                }
            }

            if (0 < data.Annotations?.Count)
            {
                var annotations = new Dictionary<string, AttributeValue>();

                foreach (var (key,value) in data.Annotations)
                {
                    if(value == null)
                        continue;

                    annotations[key] = new AttributeValue(value);
                }

                databaseData[ScoreDatabasePropertyNames.Annotations] = new AttributeValue() {M = annotations};
            }

            return new AttributeValue() {M = databaseData};
        }

        public static string ConvertToUnixTimeMilli(DateTimeOffset datetime) =>
            datetime.ToUnixTimeMilliseconds().ToString("X");

        public static DateTimeOffset ConvertFromUnixTimeMilli(string datetime) =>
            DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(datetime, NumberStyles.HexNumber));

        public static DateTimeOffset UnixTimeMillisecondsNow() =>
            DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.Now.ToUnixTimeMilliseconds());

        public static string ConvertToBase64FromSnapshotName(string snapshotName) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(snapshotName));

        public static string ConvertToSnapshotNameFromBase64(string snapshotNameBase64) =>
            Encoding.UTF8.GetString(Convert.FromBase64String(snapshotNameBase64));
    }

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
                    throw;
                }
            }
        }

        public async Task CreateAsync(Guid ownerId, string title, string description)
        {
            if (ownerId == Guid.Empty)
                throw new ArgumentException(nameof(ownerId));
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var preprocessingTitle = title.Trim();
            if (preprocessingTitle == "")
                throw new ArgumentException(nameof(title));

            var scoreCountMax = _quota.ScoreCountMax;

            var newScoreId = Guid.NewGuid();

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
                            TableName = tableName
                        }
                    },
                };
                try
                {
                    var response = await client.TransactWriteItemsAsync(new TransactWriteItemsRequest()
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

        public Task DeleteAsync(Guid ownerId, Guid scoreId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTitleAsync(Guid ownerId, Guid scoreId, string title)
        {
            throw new NotImplementedException();
        }

        public Task UpdateDescriptionAsync(Guid ownerId, Guid scoreId, string description)
        {
            throw new NotImplementedException();
        }

        public Task AddPagesAsync(Guid ownerId, Guid scoreId, List<NewScorePage> pages)
        {
            throw new NotImplementedException();
        }

        public Task RemovePagesAsync(Guid ownerId, Guid scoreId, List<int> pageIds)
        {
            throw new NotImplementedException();
        }

        public Task ReplacePagesAsync(Guid ownerId, Guid scoreId, List<PatchScorePage> pages)
        {
            throw new NotImplementedException();
        }

        public Task AddAnnotationsAsync(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAnnotationsAsync(Guid ownerId, Guid scoreId, List<int> annotationIds)
        {
            throw new NotImplementedException();
        }

        public Task ReplaceAnnotationsAsync(Guid ownerId, Guid scoreId, List<PatchScoreAnnotation> annotations)
        {
            throw new NotImplementedException();
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
                    throw;
                }
                catch (ProvisionedThroughputExceededException ex)
                {
                    throw;
                }
                catch (RequestLimitExceededException ex)
                {
                    throw;
                }
                catch (ResourceNotFoundException ex)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            throw new NotImplementedException();
        }

        public Task<ScoreDetail> GetScoreDetailAsync(Guid ownerId, Guid scoreId)
        {
            throw new NotImplementedException();
        }

        public Task<ScoreDetail> GetScoreDetailAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {
            throw new NotImplementedException();
        }

        public Task CreateSnapshotAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSnapshotAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {
            throw new NotImplementedException();
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
                        .Select(ScoreDatabaseUtils.ConvertToSnapshotNameFromBase64)
                        .ToArray();
                }
                catch (InternalServerErrorException ex)
                {
                    throw;
                }
                catch (ProvisionedThroughputExceededException ex)
                {
                    throw;
                }
                catch (RequestLimitExceededException ex)
                {
                    throw;
                }
                catch (ResourceNotFoundException ex)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
    }
}
