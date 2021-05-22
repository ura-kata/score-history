using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSnapshotSummaryGetter
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreSnapshotSummaryGetter(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task<ScoreSnapshotSummary[]> GetAsync(Guid ownerId, Guid scoreId)
        {
            return await _scoreDatabase.GetSnapshotSummariesAsync(ownerId, scoreId);
        }
    }
}
