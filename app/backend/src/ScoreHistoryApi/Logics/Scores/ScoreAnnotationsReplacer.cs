using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreAnnotationsReplacer
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreAnnotationsReplacer(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task ReplaceAnnotations(Guid ownerId, Guid scoreId, List<PatchScoreAnnotation> newAnnotations)
        {
            if (newAnnotations.Count == 0)
            {
                throw new ArgumentException(nameof(newAnnotations));
            }

            var trimmedAnnotations = new List<PatchScoreAnnotation>(newAnnotations.Count);

            for (var i = 0; i < newAnnotations.Count; i++)
            {
                var ann = newAnnotations[i];
                var trimmedContent = ann.Content?.Trim();

                if (trimmedContent is null)
                {
                    throw new ArgumentException($"{nameof(newAnnotations)}[{i}].{nameof(PatchScoreAnnotation.Content)} is null.");
                }

                trimmedAnnotations.Add(new PatchScoreAnnotation()
                {
                    TargetAnnotationId = ann.TargetAnnotationId,
                    Content = trimmedContent,
                });
            }

            await _scoreDatabase.ReplaceAnnotationsAsync(ownerId, scoreId, trimmedAnnotations);
        }

    }
}
