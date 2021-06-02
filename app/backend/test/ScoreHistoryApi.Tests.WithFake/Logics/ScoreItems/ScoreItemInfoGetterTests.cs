using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Logics.ScoreItems;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithFake.Logics.ScoreItems
{
    public class ScoreItemInfoGetterTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public ScoreItemInfoGetterTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private async Task Initialize(Guid ownerId, (Guid scoreId, Guid[] itemIds)[] ids, ScoreItemDatabase scoreItemDatabase)
        {
            try
            {
                _outputHelper.WriteLine($"{nameof(scoreItemDatabase.InitializeAsync)}: start");
                var sw = Stopwatch.StartNew();
                await scoreItemDatabase.InitializeAsync(ownerId);
                _outputHelper.WriteLine($"{nameof(scoreItemDatabase.InitializeAsync)}: {sw.Elapsed.TotalMilliseconds} msec");
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            try
            {
                _outputHelper.WriteLine($"{nameof(scoreItemDatabase.DeleteOwnerItemsAsync)}: start");
                var sw = Stopwatch.StartNew();
                await scoreItemDatabase.DeleteOwnerItemsAsync(ownerId);
                _outputHelper.WriteLine($"{nameof(scoreItemDatabase.DeleteOwnerItemsAsync)}: {sw.Elapsed.TotalMilliseconds} msec");
            }
            catch
            {
                // エラーは握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1;


            try
            {
                _outputHelper.WriteLine($"{nameof(scoreItemDatabase.CreateAsync)}: start");
                var sw = Stopwatch.StartNew();
                int i = 0;
                foreach (var (scoreId, itemIds) in ids)
                {
                    foreach (var itemId in itemIds)
                    {
                        var itemData = new ScoreItemDatabaseItemDataImage()
                        {
                            OwnerId = ownerId,
                            ScoreId = scoreId,
                            ItemId = itemId,
                            ObjName = objName,
                            OrgName = orgName,
                            Size = size,
                            Thumbnail = new ScoreItemDatabaseItemDataImageThumbnail()
                            {
                                ObjName = thumbnailObjName,
                                Size = thumbnailSize,
                            }
                        };

                        await scoreItemDatabase.CreateAsync(itemData);

                        if ((++i) % 200 == 0)
                        {
                            _outputHelper.WriteLine($"{i} / 2000");
                        }
                    }
                }
                _outputHelper.WriteLine($"{nameof(scoreItemDatabase.CreateAsync)}: {sw.Elapsed.TotalMilliseconds} msec");
            }
            catch
            {
                // エラーは握りつぶす
            }
        }

        [Fact]
        public async Task GetOwnerItemsInfoAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var scoreItemRelationTableName = "ura-kata-score-history-item-relation";
            var scoreItemDatabase = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName,
                scoreItemRelationTableName);

            var target = new ScoreItemInfoGetter(scoreItemDatabase);

            var ownerId = Guid.Parse("872c3d39-2e2e-4d70-9d6f-aaf2b20bd990");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("732e70e9-ec06-48da-a303-afd9060d5062"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
                (Guid.Parse("6b4db161-bd4d-45d9-9c1f-e6287beb18e5"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
            };

            await Initialize(ownerId, ids, scoreItemDatabase);

            var sw = Stopwatch.StartNew();
            _outputHelper.WriteLine($"{nameof(scoreItemDatabase.GetItemsAsync)}: start");

            var data = await target.GetOwnerItemsInfoAsync(ownerId);

            _outputHelper.WriteLine($"{nameof(scoreItemDatabase.GetItemsAsync)}: {sw.Elapsed.TotalMilliseconds} msec");

            Assert.Equal(2000, data.ItemInfos.Count);

            var expectedIds = ids
                .SelectMany(x => x.itemIds.Select(y => (s:x.scoreId,i: y)))
                .OrderBy(x => x)
                .ToArray();

            var actualIds = data.ItemInfos
                .Select(x => (s: x.ScoreId, i: x.ItemId))
                .OrderBy(x=>x)
                .ToArray();

            Assert.Equal(expectedIds, actualIds);
        }

        [Fact]
        public async Task GetUserItemsInfoAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var scoreItemRelationTableName = "ura-kata-score-history-item-relation";
            var scoreItemDatabase = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName,
                scoreItemRelationTableName);

            var target = new ScoreItemInfoGetter(scoreItemDatabase);

            var ownerId = Guid.Parse("cbf6f217-ca22-48ba-81f1-9b9abfa0dfbb");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("0f5c6a0a-7f17-4531-bdb1-980b6ba11858"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
                (Guid.Parse("fff568e2-ceac-4550-b63d-a8dee0962eb3"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
            };

            await Initialize(ownerId, ids, scoreItemDatabase);

            var sw = Stopwatch.StartNew();
            _outputHelper.WriteLine($"{nameof(scoreItemDatabase.GetItemsAsync)}: start");

            var data = await target.GetUserItemsInfoAsync(ownerId);

            _outputHelper.WriteLine($"{nameof(scoreItemDatabase.GetItemsAsync)}: {sw.Elapsed.TotalMilliseconds} msec");

            Assert.Equal(2000, data.ItemInfos.Count);

            var expectedIds = ids
                .SelectMany(x => x.itemIds.Select(y => (s:x.scoreId,i: y)))
                .OrderBy(x => x)
                .ToArray();

            var actualIds = data.ItemInfos
                .Select(x => (s: x.ScoreId, i: x.ItemId))
                .OrderBy(x=>x)
                .ToArray();

            Assert.Equal(expectedIds, actualIds);
        }
    }
}
