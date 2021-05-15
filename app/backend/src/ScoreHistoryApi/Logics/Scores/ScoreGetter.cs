using System;
using System.Linq;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreGetter
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreGetter(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task<ScoreSummary[]> GetScoreSummaries(Guid ownerId)
        {
            var summaries = await _scoreDatabase.GetScoreSummariesAsync(ownerId);

            return summaries.ToArray();
        }

    }
}
