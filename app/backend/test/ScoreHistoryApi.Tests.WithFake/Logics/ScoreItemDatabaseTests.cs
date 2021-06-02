using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using Xunit;
using Xunit.Abstractions;

namespace ScoreHistoryApi.Tests.WithFake.Logics
{
    public class ScoreItemDatabaseTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public ScoreItemDatabaseTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task CreateAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var scoreItemRelationTableName = "ura-kata-score-history-item-relation";
            var target = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName,
                scoreItemRelationTableName);

            var ownerId = Guid.Parse("5a56be69-af15-41a1-a879-08b6efd40eef");
            var scoreId = Guid.Parse("79867694-f52d-406c-b519-9091153cf5d3");
            var itemId = Guid.Parse("8228a9dd-d752-43f1-be84-31fce6b088d9");
            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            try
            {
                await target.DeleteAsync(ownerId, scoreId, itemId);
            }
            catch
            {
                // 握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1024;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1024;

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
            await target.CreateAsync(itemData);
        }


        [Fact]
        public async Task DeleteAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var scoreItemRelationTableName = "ura-kata-score-history-item-relation";
            var target = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName,
                scoreItemRelationTableName);

            var ownerId = Guid.Parse("a585178e-a477-4fb8-8e2a-c385a45c0d08");
            var scoreId = Guid.Parse("2533ffdd-9624-4c80-979f-95561edf5ed1");
            var itemId = Guid.Parse("67ff2dbd-c6d4-41c1-a9e5-857b56a09361");
            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1024;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1024;

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

            try
            {
                await target.CreateAsync(itemData);
            }
            catch
            {
                // エラーは握りつぶす
            }

            await target.DeleteAsync(ownerId, scoreId, itemId);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await target.GetItemAsync(ownerId, scoreId, itemId));
        }


        [Fact]
        public async Task DeleteOwnerItemsAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var scoreItemRelationTableName = "ura-kata-score-history-item-relation";
            var target = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName,
                scoreItemRelationTableName);

            var ownerId = Guid.Parse("39b6bf0e-8c39-47ce-bae5-49c557f8d2fa");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("361aaffb-6151-4bd4-a0a8-06af87a04565"),
                    new[]
                    {
                        Guid.Parse("b45282b1-88e0-4079-beaf-583f58879bd9"),
                        Guid.Parse("d41a3cd5-38a4-45bb-813a-53be5867b4ca"),
                    }),
                (Guid.Parse("a8a5e141-633b-4481-ba50-d14fdde969a7"),
                    new[]
                    {
                        Guid.Parse("daaea941-c626-4d03-9a2a-adbba45a9437"),
                        Guid.Parse("a75f6884-ae65-4eb0-91c1-5a65239319c6"),
                    }),
            };

            try
            {
                await target.InitializeAsync(ownerId);
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            var objName = ScoreItemStorageConstant.JpegFileName;
            var orgName = "origin_image.jpg";
            var size = 1024 * 1024;

            var thumbnailObjName = ScoreItemStorageConstant.ThumbnailFileName;
            var thumbnailSize = 1024;


            try
            {
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

                        await target.CreateAsync(itemData);
                    }
                }
            }
            catch
            {
                // エラーは握りつぶす
            }

            await target.DeleteOwnerItemsAsync(ownerId);

        }


        [Fact]
        public async Task GetItemsAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var scoreItemRelationTableName = "ura-kata-score-history-item-relation";
            var target = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName,
                scoreItemRelationTableName);

            var ownerId = Guid.Parse("6f76e99b-6835-4067-b4ff-22d3eb1d1c33");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("a4442515-24b2-490d-bb5b-7446e1be1e0b"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
                (Guid.Parse("9b318593-66de-45c4-a9b3-f1f5a05bb52f"),
                    Enumerable
                        .Range(0,1000)
                        .Select(x=>new Guid(x.ToString("00000000000000000000000000000000")))
                        .ToArray()),
            };

            try
            {
                _outputHelper.WriteLine($"{nameof(target.InitializeAsync)}: start");
                var sw = Stopwatch.StartNew();
                await target.InitializeAsync(ownerId);
                _outputHelper.WriteLine($"{nameof(target.InitializeAsync)}: {sw.Elapsed.TotalMilliseconds} msec");
            }
            catch
            {
                // 初期化のエラーは握りつぶす
            }

            try
            {
                _outputHelper.WriteLine($"{nameof(target.DeleteOwnerItemsAsync)}: start");
                var sw = Stopwatch.StartNew();
                await target.DeleteOwnerItemsAsync(ownerId);
                _outputHelper.WriteLine($"{nameof(target.DeleteOwnerItemsAsync)}: {sw.Elapsed.TotalMilliseconds} msec");
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
                _outputHelper.WriteLine($"{nameof(target.CreateAsync)}: start");
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

                        await target.CreateAsync(itemData);

                        if ((++i) % 200 == 0)
                        {
                            _outputHelper.WriteLine($"{i} / 2000");
                        }
                    }
                }
                _outputHelper.WriteLine($"{nameof(target.CreateAsync)}: {sw.Elapsed.TotalMilliseconds} msec");
            }
            catch
            {
                // エラーは握りつぶす
            }

            var sw2 = Stopwatch.StartNew();
            _outputHelper.WriteLine($"{nameof(target.GetItemsAsync)}: start");
            var data = await target.GetItemsAsync(ownerId);
            _outputHelper.WriteLine($"{nameof(target.GetItemsAsync)}: {sw2.Elapsed.TotalMilliseconds} msec");

            Assert.Equal(2000, data.Length);

            var expectedIds = ids
                .SelectMany(x => x.itemIds.Select(y => (s:x.scoreId,i: y)))
                .OrderBy(x => x)
                .ToArray();

            var actualIds = data
                .Select(x => (s: x.ScoreId, i: x.ItemId))
                .OrderBy(x=>x)
                .ToArray();

            Assert.Equal(expectedIds, actualIds);
        }
    }
}
