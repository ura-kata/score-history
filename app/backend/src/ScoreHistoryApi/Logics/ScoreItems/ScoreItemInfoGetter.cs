using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemInfoGetter
    {
        private readonly IScoreItemDatabase _scoreItemDatabase;

        public ScoreItemInfoGetter(IScoreItemDatabase scoreItemDatabase)
        {
            _scoreItemDatabase = scoreItemDatabase;
        }

        public async Task<ScoreItemInfo[]> GetItemInfosAsync(Guid ownerId)
        {
            throw new NotImplementedException();
        }
    }
}
