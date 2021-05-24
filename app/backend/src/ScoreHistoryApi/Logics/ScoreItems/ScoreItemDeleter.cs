using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics.ScoreItems
{
    public class ScoreItemDeleter
    {
        private readonly IScoreItemDatabase _scoreItemDatabase;

        public ScoreItemDeleter(IScoreItemDatabase scoreItemDatabase)
        {
            _scoreItemDatabase = scoreItemDatabase;
        }

        public async Task DeleteItemsAsync(Guid ownerId, DeletingScoreItems deletingScoreItems)
        {
            await _scoreItemDatabase.DeleteItemsAsync(ownerId, deletingScoreItems);
        }
    }
}
