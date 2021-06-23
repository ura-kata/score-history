using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreAnnotationAdder
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreAnnotationAdder(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration)
        {
            _dynamoDbClient = dynamoDbClient;
            _quota = quota;
            _configuration = configuration;

            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;

            var scoreDataTableName = configuration[EnvironmentNames.ScoreLargeDataDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(scoreDataTableName))
                throw new InvalidOperationException(
                    $"'{EnvironmentNames.ScoreLargeDataDynamoDbTableName}' is not found.");
            ScoreDataTableName = scoreDataTableName;
        }

        public string ScoreDataTableName { get; set; }

        public string ScoreTableName { get; set; }

        public async Task AddAnnotations(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
            {
                throw new ArgumentException(nameof(annotations));
            }

            var trimmedAnnotations = new List<NewScoreAnnotation>();

            for (var i = 0; i < annotations.Count; i++)
            {
                var ann = annotations[i];
                var trimContent = ann.Content?.Trim();
                if (trimContent is null)
                {
                    throw new ArgumentException($"{nameof(annotations)}[{i}].{nameof(NewScoreAnnotation.Content)} is null.");
                }

                trimmedAnnotations.Add(new NewScoreAnnotation()
                {
                    Content = trimContent,
                });
            }

            await AddAnnotationsAsync(ownerId, scoreId, trimmedAnnotations);
        }

        public async Task AddAnnotationsAsync(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
                throw new ArgumentException(nameof(annotations));

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

            data.Annotations ??= new List<DynamoDbScoreAnnotationV1>();

            var newAnnotations = new List<DynamoDbScoreAnnotationV1>();

            var annotationId = data.Annotations.Count == 0 ? 0 : data.Annotations.Select(x => x.Id).Max() + 1;

            var newAnnotationContentHashDic = new Dictionary<string, NewScoreAnnotation>();
            var existedContentHashSet = new HashSet<string>();
            data.Annotations.ForEach(h => existedContentHashSet.Add(h.ContentHash));

            foreach (var annotation in annotations)
            {
                var hash = DynamoDbScoreDataUtils.CalcHash(DynamoDbScoreDataUtils.AnnotationPrefix, annotation.Content);

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

            var newHash = data.CalcDataHash();

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            var annotationCountMax = _quota.AnnotationCountLimit;

            await AddAnnListAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId, newAnnotationContentHashDic);
            await UpdateAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId, newAnnotations, newHash, oldHash, now,annotationCountMax);

            static async Task AddAnnListAsync(
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId,
                Dictionary<string, NewScoreAnnotation> newAnnotations)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var chunkList = newAnnotations.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x => x.Select(y => (hash: y.x.Key, ann: y.x.Value)).ToArray())
                    .ToArray();

                foreach (var valueTuples in chunkList)
                {
                    await AddAnnList25Async(client, tableName, partitionKey, score, valueTuples);
                }

                static async Task AddAnnList25Async(
                    IAmazonDynamoDB client, string tableName, string partitionKey, string score,
                    (string hash, NewScoreAnnotation ann)[] annotations)
                {
                    Dictionary<string,List<WriteRequest>> request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = annotations.Select(a=>
                        {
                            var dataId = DynamoDbScoreDataConstant.PrefixAnnotation + score +  a.hash;
                            return new WriteRequest()
                            {
                                PutRequest = new PutRequest()
                                {
                                    Item = new Dictionary<string, AttributeValue>()
                                    {
                                        [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                        [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(dataId),
                                        [DynamoDbScoreDataPropertyNames.Content] = new AttributeValue(a.ann.Content),
                                    }
                                }
                            };
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
                Guid ownerId,
                Guid scoreId)
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var request = new GetItemRequest()
                {
                    TableName = tableName,
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
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
                Guid ownerId,
                Guid scoreId,
                List<DynamoDbScoreAnnotationV1> newAnnotations,
                string newHash,
                string oldHash,
                DateTimeOffset now,
                int annotationCountMax
                )
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.OwnerId] = new AttributeValue(partitionKey),
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
    }
}
