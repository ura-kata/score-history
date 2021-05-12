using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using ScoreHistoryApi.Factories;
using ScoreHistoryApi.Logics;
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
    }
}
