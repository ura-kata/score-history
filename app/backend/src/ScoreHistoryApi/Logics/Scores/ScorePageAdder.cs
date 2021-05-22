using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScorePageAdder
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScorePageAdder(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task AddPages(Guid ownerId, Guid scoreId, List<NewScorePage> pages)
        {
            if (pages.Count == 0)
            {
                throw new ArgumentException(nameof(pages));
            }

            var trimmedPages = new List<NewScorePage>();

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var trimmedPage = page.Page?.Trim();
                if (trimmedPage is null)
                {
                    throw new ArgumentException($"{nameof(pages)}[{i}] is null.");
                }

                trimmedPages.Add(new NewScorePage()
                {
                    Page = trimmedPage,
                    ItemId = page.ItemId,
                });
            }

            await _scoreDatabase.AddPagesAsync(ownerId, scoreId, trimmedPages);
        }
    }
}
