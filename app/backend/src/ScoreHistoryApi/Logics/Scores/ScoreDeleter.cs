using System;
using System.Threading.Tasks;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreDeleter
    {
        private readonly IScoreDatabase _scoreDatabase;
        private readonly IScoreSnapshotStorage _snapshotStorage;

        public ScoreDeleter(IScoreDatabase scoreDatabase, IScoreSnapshotStorage snapshotStorage)
        {
            _scoreDatabase = scoreDatabase;
            _snapshotStorage = snapshotStorage;
        }

        public async Task DeleteAsync(Guid ownerId, Guid scoreId)
        {
            // ここでは DynamoDB の楽譜の構造のみを削除する
            // Item の削除は別の API で削除を行う

            await _scoreDatabase.DeleteAsync(ownerId, scoreId);
            await _snapshotStorage.DeleteAllAsync(ownerId, scoreId);
        }

    }
}
