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
    public class ScoreAnnotationReplacer
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;

        public ScoreAnnotationReplacer(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration)
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

        public async Task ReplaceAnnotations(Guid ownerId, Guid scoreId, List<PatchScoreAnnotation> newAnnotations)
        {
            if (newAnnotations.Count == 0)
            {
                throw new ArgumentException(nameof(newAnnotations));
            }

            var trimmedAnnotations = new List<PatchScoreAnnotation>(newAnnotations.Count);

            for (var i = 0; i < newAnnotations.Count; i++)
            {
                var ann = newAnnotations[i];
                var trimmedContent = ann.Content?.Trim();

                if (trimmedContent is null)
                {
                    throw new ArgumentException($"{nameof(newAnnotations)}[{i}].{nameof(PatchScoreAnnotation.Content)} is null.");
                }

                trimmedAnnotations.Add(new PatchScoreAnnotation()
                {
                    TargetAnnotationId = ann.TargetAnnotationId,
                    Content = trimmedContent,
                });
            }

            await ReplaceAnnotationsAsync(ownerId, scoreId, trimmedAnnotations);
        }


        public async Task ReplaceAnnotationsAsync(Guid ownerId, Guid scoreId, List<PatchScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
                throw new ArgumentException(nameof(annotations));

            var (data, oldHash) = await GetAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId);

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

                var hash = DynamoDbScoreDataUtils.CalcHash(DynamoDbScoreDataUtils.AnnotationPrefix, ann.Content);

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

            var newHash = data.CalcDataHash();

            var now = ScoreDatabaseUtils.UnixTimeMillisecondsNow();

            await UpdateAsync(_dynamoDbClient, ScoreTableName, ownerId, scoreId, replacingAnnotations, newHash, oldHash, now);

            await AddAnnListAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId, addAnnData);
            await RemoveAnnListAsync(_dynamoDbClient, ScoreDataTableName, ownerId, scoreId, removeAnnData);

            static async Task<(DynamoDbScoreDataV1 data, string hash)> GetAsync(
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
                        [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.SortKey] =
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
                Guid ownerId,
                Guid scoreId,
                List<(DynamoDbScoreAnnotationV1 data, int targetIndex)> replacingAnnotations,
                string newHash,
                string oldHash,
                DateTimeOffset now
            )
            {
                var partitionKey = ScoreDatabaseUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var updateAt = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

                var replacingValues = replacingAnnotations
                    .Select(x => (key: ":newAnn" + x.targetIndex, value: ConvertFromAnnotation(x.data), x.targetIndex))
                    .ToArray();
                var request = new UpdateItemRequest()
                {
                    Key = new Dictionary<string, AttributeValue>()
                    {
                        [DynamoDbScorePropertyNames.PartitionKey] = new AttributeValue(partitionKey),
                        [DynamoDbScorePropertyNames.SortKey] =
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
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId,
                Dictionary<string, PatchScoreAnnotation> newAnnotations)
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
                    (string hash, PatchScoreAnnotation ann)[] annotations)
                {
                    Dictionary<string,List<WriteRequest>> request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = annotations.Select(a=>
                        {
                            var dataId = DynamoDbScoreDataConstant.PrefixAnnotation + score + a.hash;
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


            static async Task RemoveAnnListAsync(
                IAmazonDynamoDB client, string tableName, Guid ownerId, Guid scoreId,
                HashSet<string> removeAnnotations)
            {
                const int chunkSize = 25;

                var partitionKey = DynamoDbScoreDataUtils.ConvertToPartitionKey(ownerId);
                var score = ScoreDatabaseUtils.ConvertToBase64(scoreId);

                var chunkList = removeAnnotations.Select((x, index) => (x, index))
                    .GroupBy(x => x.index / chunkSize)
                    .Select(x => x.Select(y => y.x).ToArray())
                    .ToArray();

                foreach (var hashList in chunkList)
                {
                    await RemoveAnnList25Async(client, tableName, partitionKey, score, hashList);
                }

                static async Task RemoveAnnList25Async(
                    IAmazonDynamoDB client, string tableName, string partitionKey, string score,
                    string[] annotations)
                {
                    var request = new Dictionary<string, List<WriteRequest>>()
                    {
                        [tableName] = annotations.Select(hash=>
                        {
                            var dataId = DynamoDbScoreDataConstant.PrefixAnnotation + score + hash;
                            return new WriteRequest()
                            {
                                DeleteRequest = new DeleteRequest()
                                {
                                    Key = new Dictionary<string, AttributeValue>()
                                    {
                                        [DynamoDbScoreDataPropertyNames.OwnerId] = new AttributeValue(partitionKey),
                                        [DynamoDbScoreDataPropertyNames.DataId] = new AttributeValue(dataId),
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
        }

    }
}
