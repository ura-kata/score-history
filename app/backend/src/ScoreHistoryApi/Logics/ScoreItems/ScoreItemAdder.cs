using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemAdder
    {
        private readonly IScoreItemDatabase _scoreItemDatabase;
        private readonly IScoreItemStorage _scoreItemStorage;
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreItemAdder(IScoreItemDatabase scoreItemDatabase, IScoreItemStorage scoreItemStorage, IScoreDatabase scoreDatabase)
        {
            _scoreItemDatabase = scoreItemDatabase;
            _scoreItemStorage = scoreItemStorage;
            _scoreDatabase = scoreDatabase;
        }

        public async Task<UploadedScoreObjectResult> AddAsync(Guid ownerId, UploadingScoreItem uploadingScoreItem)
        {
            var scoreId = uploadingScoreItem.ScoreId;

            var score = await _scoreDatabase.GetDynamoDbScoreDataAsync(ownerId, scoreId);

            var access = score.score.Access == ScoreDatabaseConstant.ScoreAccessPublic
                ? ScoreObjectAccessControls.Public
                : ScoreObjectAccessControls.Private;

            await using var stream = uploadingScoreItem.Item.OpenReadStream();
            var data = new byte[stream.Length];
            await stream.ReadAsync(data);

            var orgName = uploadingScoreItem.Item.FileName;

            var response = await _scoreItemStorage.SaveObjectAsync(ownerId, scoreId, data, access);

            var thumbnail = response.Extra switch
            {
                ImagePngExtra p => new ScoreItemDatabaseItemDataImageThumbnail()
                {
                    Size = p.Thumbnail.Size,
                    ObjName = p.Thumbnail.ObjectName,
                },
                Thumbnail t => new ScoreItemDatabaseItemDataImageThumbnail()
                {
                    Size = t.Size,
                    ObjName = t.ObjectName,
                },
                _ => null,
            };

            var totalSize = response.Size + (thumbnail?.Size ?? 0);

            ScoreItemDatabaseItemDataBase itemData = new ScoreItemDatabaseItemDataImage()
            {
                OwnerId = ownerId,
                ScoreId = scoreId,
                OrgName = orgName,
                ItemId = response.ItemId,
                ObjName = response.ObjectName,
                Size = response.Size,
                Thumbnail = thumbnail,
                TotalSize = totalSize,
            };
            await _scoreItemDatabase.CreateAsync(itemData);

            return new UploadedScoreObjectResult()
            {
                ImageItemInfo = new ScoreImageItemInfo()
                {
                    Size = itemData.Size,
                    Thumbnail = thumbnail?.ObjName,
                    ObjectName = itemData.ObjName,
                    ItemId = itemData.ItemId,
                    OriginalName = orgName,
                    ScoreId = scoreId,
                    TotalSize = itemData.TotalSize,
                    ThumbnailSize = thumbnail?.Size ?? 0,
                },
            };
        }
    }
}
