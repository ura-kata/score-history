using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreDetailGetter
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreDetailGetter(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task<ScoreDetail> GetScoreSummaries(Guid ownerId, Guid scoreId)
        {
            var (data, annotations, access) = await _scoreDatabase.GetDatabaseScoreRecordAsync(ownerId, scoreId);

            return ScoreDetail.Create(data, annotations, access);
        }

    }
}
