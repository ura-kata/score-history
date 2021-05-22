using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScorePageRemover
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScorePageRemover(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task RemovePages(Guid ownerId, Guid scoreId, List<long> pageIds)
        {
            if (pageIds.Count == 0)
            {
                throw new ArgumentException(nameof(pageIds));
            }

            await _scoreDatabase.RemovePagesAsync(ownerId, scoreId, pageIds);
        }
    }
}
