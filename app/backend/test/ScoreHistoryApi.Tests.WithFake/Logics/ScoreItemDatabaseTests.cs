using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using Xunit;

// TODO 冪等性が担保されるようにテストを修正する
namespace ScoreHistoryApi.Tests.WithFake.Logics
{
    public class ScoreItemDatabaseTests
    {
        [Fact]
        public async Task CreateAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var target = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("ceb9b868-0b9b-4c8d-be70-fba729b47b77");
            var scoreId = Guid.Parse("e7bdb2c7-6b0a-4c4b-8246-0e118f4cc3f2");
            var itemId = Guid.Parse("056d759e-38f5-4379-8e0e-a787bd666cc0");
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
            await target.CreateAsync(itemData);
        }


        [Fact]
        public async Task DeleteAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var target = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("2700d519-2b1d-4b3a-b547-62bc0d7bc354");
            var scoreId = Guid.Parse("2b3d6034-375d-46d5-a82e-8a4d15c77b1e");
            var itemId = Guid.Parse("bc941740-9f98-41d9-ae97-ec47ef6e7555");
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

            await target.DeleteAsync(ownerId, itemId);

        }


        [Fact]
        public async Task DeleteOwnerItemsAsyncTest()
        {
            var factory = new DynamoDbClientFactory().SetEndpointUrl(new Uri("http://localhost:18000"));
            var tableName = "ura-kata-score-history-item";
            var target = new ScoreItemDatabase(new ScoreQuota(), factory.Create(), tableName);

            var ownerId = Guid.Parse("2700d519-2b1d-4b3a-b547-62bc0d7bc354");

            var ids = new (Guid scoreId, Guid[] itemIds)[]
            {
                (Guid.Parse("2b3d6034-375d-46d5-a82e-8a4d15c77b1e"),
                    new[]
                    {
                        Guid.Parse("bc941740-9f98-41d9-ae97-ec47ef6e7555"),
                        Guid.Parse("762b92da-66da-4857-99c2-7974c79b7670"),
                    }),
                (Guid.Parse("c687be8c-c8c1-42e4-9b1c-caf3ed21c4a0"),
                    new[]
                    {
                        Guid.Parse("44e1c4e0-59d9-45ff-b38a-bc588c599856"),
                        Guid.Parse("4d9cf069-1623-410d-84cf-006bbcc78780"),
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
    }
}
