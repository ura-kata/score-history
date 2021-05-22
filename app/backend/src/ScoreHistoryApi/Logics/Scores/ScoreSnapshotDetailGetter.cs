using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSnapshotDetailGetter
    {
        private readonly IScoreSnapshotStorage _snapshotStorage;

        public ScoreSnapshotDetailGetter(IScoreSnapshotStorage snapshotStorage)
        {
            _snapshotStorage = snapshotStorage;
        }

        public async Task<ScoreSnapshotDetail> GetScoreSummaries(Guid ownerId, Guid scoreId, Guid snapshotId)
        {
            return await _snapshotStorage.GetAsync(ownerId, scoreId, snapshotId);
        }
    }
}
