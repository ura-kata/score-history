using System;
using System.Linq;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemInfoGetter
    {
        private readonly IScoreItemDatabase _scoreItemDatabase;

        public ScoreItemInfoGetter(IScoreItemDatabase scoreItemDatabase)
        {
            _scoreItemDatabase = scoreItemDatabase;
        }

        public async Task<OwnerItemsInfo> GetOwnerItemsInfoAsync(Guid ownerId)
        {
            var itemDataList = await _scoreItemDatabase.GetItemsAsync(ownerId);

            var totalSize = itemDataList.Sum(x => x.TotalSize);

            var items = itemDataList.Select(ConvertToItemInfo).ToList();

            return new OwnerItemsInfo()
            {
                TotalSize = totalSize,
                ItemInfos = items,
            };
        }

        public async Task<UserItemsInfo> GetUserItemsInfoAsync(Guid ownerId)
        {
            var itemDataList = await _scoreItemDatabase.GetItemsAsync(ownerId);

            var totalSize = itemDataList.Sum(x => x.TotalSize);

            var items = itemDataList.Select(ConvertToItemInfo).ToList();

            return new UserItemsInfo()
            {
                TotalSize = totalSize,
                ItemInfos = items,
            };
        }

        private ScoreItemInfoBase ConvertToItemInfo(ScoreItemDatabaseItemDataBase itemData)
        {
            if (itemData is ScoreItemDatabaseItemDataImage itemDataImage)
            {
                return new ScoreImageItemInfo()
                {
                    ScoreId = itemDataImage.ScoreId,
                    ItemId = itemDataImage.ItemId,
                    Size = itemDataImage.Size,
                    TotalSize = itemDataImage.TotalSize,
                    ObjectName = itemDataImage.ObjName,
                    OriginalName = itemDataImage.OrgName,
                    Thumbnail = itemDataImage.Thumbnail.ObjName,
                    ThumbnailSize = itemDataImage.Thumbnail.Size,
                };
            }

            throw new ArgumentException();
        }
    }
}
