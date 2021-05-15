using System;
using System.Threading.Tasks;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreDeleter
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreDeleter(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task DeleteAsync(Guid ownerId, Guid scoreId)
        {
            // ここでは DynamoDB の楽譜の構造のみを削除する
            // Item の削除は別の API で削除を行う

            await _scoreDatabase.DeleteAsync(ownerId, scoreId);
        }

    }
}
