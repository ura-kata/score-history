using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using ScoreHistoryApi.Logics.DynamoDb;
using ScoreHistoryApi.Logics.DynamoDb.Models;
using ScoreHistoryApi.Logics.DynamoDb.PropertyNames;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreAnnotationAdder
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly IScoreQuota _quota;
        private readonly IConfiguration _configuration;
        private readonly IScoreCommonLogic _commonLogic;

        public ScoreAnnotationAdder(IAmazonDynamoDB dynamoDbClient, IScoreQuota quota, IConfiguration configuration,
            IScoreCommonLogic commonLogic)
        {
            _dynamoDbClient = dynamoDbClient;
            _quota = quota;
            _configuration = configuration;
            _commonLogic = commonLogic;

            var tableName = configuration[EnvironmentNames.ScoreDynamoDbTableName];
            if (string.IsNullOrWhiteSpace(tableName))
                throw new InvalidOperationException($"'{EnvironmentNames.ScoreDynamoDbTableName}' is not found.");
            ScoreTableName = tableName;
        }

        public string ScoreTableName { get; set; }

        public async Task AddAnnotationsAsync(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations)
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
                    throw new ArgumentException(
                        $"{nameof(annotations)}[{i}].{nameof(NewScoreAnnotation.Content)} is null.");
                }

                if (_quota.AnnotationLengthMax < trimContent.Length)
                {
                    throw new ArgumentException(
                        $"{nameof(annotations)}[{i}].{nameof(NewScoreAnnotation.Content)} is too long.");
                }

                trimmedAnnotations.Add(new NewScoreAnnotation()
                {
                    Content = trimContent,
                });
            }

            await AddAnnotationsInnerAsync(ownerId, scoreId, trimmedAnnotations);
        }

        public async Task AddAnnotationsInnerAsync(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
                throw new ArgumentException(nameof(annotations));

            var (oldAnnotations, oldAnnotationCount oldLock, existedChunks) = await GetAsync(ownerId, scoreId);

            var sortedIds = oldAnnotations.OrderBy(x => x.Id).Select(x=>x.Id).ToArray();

            long next = 0;
            int idIndex = 0;
            long NewId()
            {
                for (; idIndex < sortedIds.Length; ++idIndex, ++next)
                {
                    var id = sortedIds[idIndex];
                    if (next == id)
                    {
                        continue;
                    }

                    return next++;
                }
                return next++;
            }

            var newAnnotations = new List<AnnotationOnMain>();
            foreach (var annotation in annotations)
            {
                newAnnotations.Add(new AnnotationOnMain()
                {
                    Id = NewId(),
                    Length = annotation.Content.Length,
                });
            }


            var now = _commonLogic.Now;

            var annotationCountMax = _quota.AnnotationCountMax;

            await UpdateAsync(ownerId, scoreId, newAnnotations, oldLock, now, annotationCountMax);

        }


        async Task<(AnnotationOnMain[] annotations, long annotationCount, long optimisticLock, string[] existedChunks)> GetAsync(
            Guid ownerId,
            Guid scoreId)
        {
            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);
            var score = _commonLogic.ConvertIdFromGuid(scoreId);

            var request = new QueryRequest()
            {
                TableName = ScoreTableName,
                KeyConditionExpression = "#o = :o and begins_with(#s, :s)",
                ProjectionExpression = "#o,#s,#d.#d_a,#d.#d_ac,#l",
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    ["#o"] = ScoreMainPn.PartitionKey,
                    ["#s"] = ScoreMainPn.SortKey,
                    ["#d"] = ScoreMainPn.Data,
                    ["#d_a"] = ScoreMainPn.DataPn.Annotation,
                    ["#d_ac"] = ScoreMainPn.DataPn.AnnotationCount,
                    ["#l"] = ScoreMainPn.Lock,
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    [":o"] = new(partitionKey),
                    [":s"] = new(score),
                },
            };
            var response = await _dynamoDbClient.QueryAsync(request);

            if (response.Items.Count == 0)
            {
                throw new InvalidOperationException("not found.");
            }

            var items = response.Items.Select(x =>
            {
                var o = x[ScoreMainPn.PartitionKey].S;
                var s = x[ScoreMainPn.SortKey].S;
                return (o, s, value: x);
            }).ToArray();

            var main = items.First(x => x.s == score);
            var substStart = score.Length + SortDelimiter.Annotation.Length;
            var chunks = items.Select(x => x.s.Substring(substStart)).ToArray();

            if (!main.value.TryGetValue(ScoreMainPn.Data, out var data))
            {
                throw new InvalidOperationException("not found.");
            }

            if (!data.M.TryGetValue(ScoreMainPn.Lock, out var optimisticLockValue))
            {
                throw new InvalidOperationException("not found.");
            }

            if (!data.M.TryGetValue(ScoreMainPn.DataPn.Annotation, out var annotationValue))
            {
                throw new InvalidOperationException("not found.");
            }

            if (!data.M.TryGetValue(ScoreMainPn.DataPn.AnnotationCount, out var annotationCountValue))
            {
                throw new InvalidOperationException("not found.");
            }


            if (!long.TryParse(optimisticLockValue.N, out var optimisticLock))
            {
                throw new InvalidOperationException("not found.");
            }

            var annotations = annotationValue.L.Select(ann =>
            {
                ann.M.TryGetValue(ScoreMainPn.DataPn.AnnotationPn.Id, out var idValue);
                ann.M.TryGetValue(ScoreMainPn.DataPn.AnnotationPn.Length, out var lengthValue);
                long.TryParse(idValue?.N, out var id);
                long.TryParse(lengthValue?.N, out var length);

                return new AnnotationOnMain()
                {
                    Id = id,
                    Length = length,
                };
            }).ToArray();

            long.TryParse(annotationCountValue.N, out var annotationCount);


            return (annotations, annotationCount, optimisticLock, chunks);
        }


        async Task UpdateAsync(
            Guid ownerId,
            Guid scoreId,
            List<AnnotationOnMain> newAnnotations,
            long oldLock,
            DateTimeOffset now,
            int annotationCountMax
        )
        {
            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);
            var score = _commonLogic.ConvertIdFromGuid(scoreId);

            var updateAt = now.ToUnixTimeMilliseconds();

            var tableName = ScoreTableName;

            var request = new UpdateItemRequest()
            {
                Key = new Dictionary<string, AttributeValue>()
                {
                    [ScoreMainPn.PartitionKey] = new(partitionKey),
                    [ScoreMainPn.SortKey] = new(score),
                },
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    ["#ua"] = ScoreMainPn.UpdateAt,
                    ["#d"] = ScoreMainPn.Data,
                    ["#a"] = ScoreMainPn.DataPn.Annotation,
                    ["#ac"] = ScoreMainPn.DataPn.AnnotationCount,
                    ["#l"] = ScoreMainPn.Lock,
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    [":newAnn"] = new (){L= newAnnotations.Select(x=>new AttributeValue()
                    {
                        M = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreMainPn.DataPn.AnnotationPn.Id] = new (){N = x.Id.ToString()},
                            [ScoreMainPn.DataPn.AnnotationPn.Length] = new (){N = x.Length.ToString()},
                        }
                    }).ToList()},
                    [":ua"] = new (){N = updateAt.ToString()},
                    [":inc"] = new (){N = "1"},
                    [":l"] = new (){N = oldLock.ToString()},
                    [":addCount"] = new (){N = oldLock.ToString()},
                    [":countMax"] = new() {N = (annotationCountMax - newAnnotations.Count).ToString()},
                },
                ConditionExpression = "#l = :l and #data.#ac < :countMax",
                UpdateExpression =
                    "SET #ua = :ua, #d.#a = list_append(#d.#a, :newAnn) ADD #d.#ac :addCount, #l :inc",
                TableName = tableName,
            };
            try
            {
                await _dynamoDbClient.UpdateItemAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
