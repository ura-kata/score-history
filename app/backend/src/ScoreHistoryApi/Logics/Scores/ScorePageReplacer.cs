using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScorePageReplacer
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScorePageReplacer(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task ReplacePages(Guid ownerId, Guid scoreId, List<PatchScorePage> pages)
        {
            if (pages.Count == 0)
            {
                throw new ArgumentException(nameof(pages));
            }

            var trimmedPages = new List<PatchScorePage>();

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var trimmedPage = page.Page?.Trim();

                if (trimmedPage is null)
                {
                    throw new ArgumentException($"{nameof(pages)}[{i}].Page is null.");
                }

                var trimmedObjectName = page.ObjectName?.Trim();
                if (string.IsNullOrWhiteSpace(trimmedObjectName))
                {
                    throw new ArgumentException($"{nameof(pages)}[{i}].ObjectName is empty.");
                }

                trimmedPages.Add(new PatchScorePage()
                {
                    TargetPageId = page.TargetPageId,
                    Page = trimmedPage,
                    ItemId = page.ItemId,
                    ObjectName = trimmedObjectName,
                });
            }

            await _scoreDatabase.ReplacePagesAsync(ownerId, scoreId, trimmedPages);
        }
    }
}
