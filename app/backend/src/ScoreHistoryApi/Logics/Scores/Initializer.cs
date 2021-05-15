using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.Exceptions;

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
            try
            {
                await _scoreDatabase.InitializeAsync(ownerId);
            }
            catch (AlreadyInitializedException ex)
            {

                // 初期化済み
            }

            try
            {
                await _scoreItemDatabase.InitializeAsync(ownerId);
            }
            catch (AlreadyInitializedException ex)
            {

                // 初期化済み
            }
        }
    }
}
