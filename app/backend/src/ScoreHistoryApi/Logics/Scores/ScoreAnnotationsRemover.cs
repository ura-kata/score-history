using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreAnnotationsRemover
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreAnnotationsRemover(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task RemoveAnnotations(Guid ownerId, Guid scoreId, List<long> annotationIds)
        {
            if (annotationIds.Count == 0)
            {
                throw new ArgumentException(nameof(annotationIds));
            }

            await _scoreDatabase.RemoveAnnotationsAsync(ownerId, scoreId, annotationIds);
        }

    }
}
