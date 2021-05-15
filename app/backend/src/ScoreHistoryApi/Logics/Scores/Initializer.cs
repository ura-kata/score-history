using System;
using System.Threading.Tasks;

namespace ScoreHistoryApi.Logics.Scores
{
    public class Initializer
    {
        private readonly IScoreDatabase _scoreDatabase;
        private readonly IScoreItemDatabase _scoreItemDatabase;

        public Initializer(IScoreDatabase scoreDatabase, IScoreItemDatabase scoreItemDatabase)
        {
            _scoreDatabase = scoreDatabase;
            _scoreItemDatabase = scoreItemDatabase;
        }

        public async Task Initialize(Guid ownerId)
        {
            await _scoreDatabase.InitializeAsync(ownerId);
            await _scoreItemDatabase.InitializeAsync(ownerId);
        }
    }
}
