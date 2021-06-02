#nullable enable

using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreCreator
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreCreator(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task<NewlyScore> CreateAsync(Guid ownerId, NewScore newScore)
        {
            return await _scoreDatabase.CreateAsync(ownerId, newScore.Title, newScore.Description);
        }
    }
}
