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

            var (oldAnnotations, oldAnnotationCount, oldLock, existedChunks) = await GetAsync(ownerId, scoreId);

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

            var annotationOnMains = new List<AnnotationOnMain>();
            var annotationOnDatas = new List<AnnotationOnData>();
            foreach (var annotation in annotations)
            {
                var id = NewId();
                annotationOnMains.Add(new AnnotationOnMain()
                {
                    Id = id,
                    Length = annotation.Content.Length,
                });
                annotationOnDatas.Add(new AnnotationOnData()
                {
                    Id = id,
                    Content = annotation.Content,
                });
            }


            var now = _commonLogic.Now;
            var timeout = now.AddSeconds(10);

            // このリクエスト処理を完了しなければならない時間
            // timeout と同じ時間にしないのは次のリクエストとアノテーションデータ更新部分 (UpdateAnnotationsAsync) が
            // 確実に並列に実行されないことを保証するため
            var processTimeout = now.AddSeconds(5);
            var newLock = _commonLogic.NewGuid();

            var annotationCountMax = _quota.AnnotationCountMax;
            var newAnnotationCount = annotations.Count;

            var existedChunkSet = new HashSet<string>(existedChunks.Distinct());

            await TransactionStartAndCheckAsync(
                ownerId, scoreId, oldLock, newLock, now, timeout,
                annotationCountMax, newAnnotationCount);

            await UpdateAnnotationsAsync(ownerId, scoreId, existedChunkSet, annotationOnDatas, processTimeout);

            var now2 = _commonLogic.Now;

            if (processTimeout <= now2)
            {
                // アノテーションデータの保存に時間がかかりタイムアウトが発生するまでに処理が終わっていない場合
                // 次のリクエストで来ている可能性があるのでこのリクエストは失敗とする
                throw new InvalidOperationException("timeout");
            }
            var newLock2 = _commonLogic.NewGuid();

            await UpdateMainAndTransactionCommitAsync(
                ownerId, scoreId, annotationOnMains,newLock, newLock2, now2, annotationCountMax);

        }

        async Task UpdateAnnotationsAsync(Guid ownerId, Guid scoreId, HashSet<string> oldChunks, List<AnnotationOnData> newAnnotations, DateTimeOffset timeout)
        {
            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);
            var score = _commonLogic.ConvertIdFromGuid(scoreId);

            const int oneChunkLength = 240;
            const int oneChunkCountMax = 200;

            var chunkDataList = newAnnotations.SelectMany(an =>
            {
                var extraCount = Math.Max(0, (int) Math.Ceiling(an.Content.Length / (double) oneChunkLength));
                var chunks = Enumerable.Range(0, extraCount).Select(index =>
                    {
                        var chunk = $"{(an.Id / oneChunkCountMax):00}{index:0}";
                        var start = index * oneChunkLength;
                        var length = Math.Min(an.Content.Length - start, oneChunkLength);
                        var content = an.Content.Substring(start, length);
                        return (chunk,id: an.Id, content);
                    })
                    .ToArray();
                return chunks;
            }).ToArray();

            var chunkDataGroup = chunkDataList.GroupBy(x => x.chunk, x => x);

            foreach (var chunkData in chunkDataGroup)
            {
                var chunk = chunkData.Key;

                if (oldChunks.Contains(chunk))
                {
                    UpdateItemRequest request = new()
                    {
                        TableName = ScoreTableName,
                        Key = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreAnnotationPn.PartitionKey] = new(partitionKey),
                            [ScoreAnnotationPn.SortKey] = new(score + SortDelimiter.Annotation + chunk),
                        },
                        ExpressionAttributeNames = new Dictionary<string, string>()
                        {
                            ["#a"] = ScoreAnnotationPn.Annotation,
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>(),
                    };

                    foreach (var data in chunkData)
                    {
                        var id = data.id.ToString("00000");

                        request.ExpressionAttributeNames["#" + id] = id;
                        request.ExpressionAttributeValues[":" + id] = new(data.content);
                    }

                    request.UpdateExpression = "SET " + string.Join(", ", chunkData.Select(x =>
                    {
                        var id = x.id.ToString("00000");
                        return $"#a.#{id} = :{id}";
                    }));

                    var now = _commonLogic.Now;
                    if (timeout <= now)
                    {
                        // タイムアウトの時間を過ぎている場合次のリクエストが来ている可能性があるので
                        // 今回のリクエストは失敗とする
                        throw new InvalidOperationException("Timeout");
                    }

                    try
                    {
                        await _dynamoDbClient.UpdateItemAsync(request);
                    }
                    catch (Exception)
                    {
                        // TODO Retry をする
                        throw;
                    }
                }
                else
                {
                    var annotation = new Dictionary<string, AttributeValue>();

                    foreach (var data in chunkData)
                    {
                        var id = data.id.ToString("00000");
                        annotation[id] = new AttributeValue(data.content);
                    }

                    PutItemRequest request = new()
                    {
                        TableName = ScoreTableName,
                        Item = new Dictionary<string, AttributeValue>()
                        {
                            [ScoreAnnotationPn.PartitionKey] = new(partitionKey),
                            [ScoreAnnotationPn.SortKey] = new(score + SortDelimiter.Annotation + chunk),
                            [ScoreAnnotationPn.Annotation] = new(){M = annotation},
                        },
                    };

                    var now = _commonLogic.Now;
                    if (timeout <= now)
                    {
                        // タイムアウトの時間を過ぎている場合次のリクエストが来ている可能性があるので
                        // 今回のリクエストは失敗とする
                        throw new InvalidOperationException("Timeout");
                    }
                    try
                    {
                        await _dynamoDbClient.PutItemAsync(request);
                    }
                    catch (Exception)
                    {
                        // TODO Retry をする
                        throw;
                    }
                }
            }

        }


        async Task<(AnnotationOnMain[] annotations, long annotationCount, string optimisticLock, string[] existedChunks)> GetAsync(
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
            var chunks = items
                .Where(x=>x.s != score)
                .Select(x => x.s.Substring(substStart))
                .ToArray();

            if (!main.value.TryGetValue(ScoreMainPn.Data, out var data))
            {
                throw new InvalidOperationException("not found.");
            }

            if (!main.value.TryGetValue(ScoreMainPn.Lock, out var optimisticLockValue))
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

            var optimisticLock = optimisticLockValue.S;


            return (annotations, annotationCount, optimisticLock, chunks);
        }

        async Task TransactionStartAndCheckAsync(
            Guid ownerId,
            Guid scoreId,
            string oldLock,
            Guid newLock,
            DateTimeOffset now,
            DateTimeOffset timeout,
            int annotationCountMax,
            int newAnnotationCount
        )
        {
            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);
            var score = _commonLogic.ConvertIdFromGuid(scoreId);

            var newLockValue = _commonLogic.ConvertIdFromGuid(newLock);
            var timeoutValue = timeout.ToUnixTimeMilliseconds().ToString();
            var nowValue = now.ToUnixTimeMilliseconds().ToString();

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
                    ["#l"] = ScoreMainPn.Lock,
                    ["#xt"] = ScoreMainPn.TransactionTimeout,
                    ["#xs"] = ScoreMainPn.TransactionStart,
                    ["#d"] = ScoreMainPn.Data,
                    ["#ac"] = ScoreMainPn.DataPn.AnnotationCount,
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
                {
                    [":new"] = new (){S = newLockValue},
                    [":old"] = new (){S = oldLock},
                    [":timeout"] = new (){N=timeoutValue},
                    [":now"] = new (){N=nowValue},
                    [":countMax"] = new() {N = (annotationCountMax - newAnnotationCount).ToString()},
                },
                // もしもリクエストが完了する前に次のリクエストがきて、かつトランザクションを開始してしまったときに
                // 現在のリクエストが失敗するようにするために xs を設定する
                ConditionExpression = "#l = :old and #xt <= :now and #d.#ac < :countMax",
                UpdateExpression = "SET #l = :new, #xt = :timeout, #xs = :now",
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


        async Task UpdateMainAndTransactionCommitAsync(
            Guid ownerId,
            Guid scoreId,
            List<AnnotationOnMain> newAnnotations,
            Guid oldLock,
            Guid newLock,
            DateTimeOffset now,
            int annotationCountMax
        )
        {
            var partitionKey = PartitionPrefix.Score + _commonLogic.ConvertIdFromGuid(ownerId);
            var score = _commonLogic.ConvertIdFromGuid(scoreId);

            var updateAt = now.ToUnixTimeMilliseconds();

            var oldLockValue = _commonLogic.ConvertIdFromGuid(oldLock);
            var newLockValue = _commonLogic.ConvertIdFromGuid(newLock);

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
                    ["#xt"] = ScoreMainPn.TransactionTimeout,
                    ["#xs"] = ScoreMainPn.TransactionStart,
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
                    [":oldL"] = new (){S = oldLockValue},
                    [":newL"] = new (){S = newLockValue},
                    [":addCount"] = new (){N = newAnnotations.Count.ToString()},
                    [":countMax"] = new() {N = (annotationCountMax - newAnnotations.Count).ToString()},
                    [":zero"] = new() {N = "0"},
                },
                // 最終的な更新処理の終了である Main の更新はトランザクションスタート <= now < トランザクションタイムアウト を
                // 満たしていなければ次のリクエストが来ていないと言えないので失敗するようにする
                // API のリクエスト時点で Lock の値を含めるようにすればさらにいい
                ConditionExpression = "#l = :oldL and #d.#ac < :countMax and #xs <= :ua and :ua < #xt",
                UpdateExpression =
                    "SET #ua = :ua, #d.#a = list_append(#d.#a, :newAnn), #xs = :zero, #xt = :zero, #l = :newL ADD #d.#ac :addCount",
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
