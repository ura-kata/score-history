using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreAnnotationAdder
    {
        private readonly IScoreDatabase _scoreDatabase;

        public ScoreAnnotationAdder(IScoreDatabase scoreDatabase)
        {
            _scoreDatabase = scoreDatabase;
        }

        public async Task AddAnnotations(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations)
        {
            if (annotations.Count == 0)
            {
                throw new ArgumentException(nameof(annotations));
            }

            var trimmedAnnotations = new List<NewScoreAnnotation>();

            for (var i = 0; i < annotations.Count; i++)
            {
                var ann = annotations[i];
                var trimContent = ann.Content?.Trim();
                if (trimContent is null)
                {
                    throw new ArgumentException($"{nameof(annotations)}[{i}].{nameof(NewScoreAnnotation.Content)} is null.");
                }

                trimmedAnnotations.Add(new NewScoreAnnotation()
                {
                    Content = trimContent,
                });
            }

            await _scoreDatabase.AddAnnotationsAsync(ownerId, scoreId, trimmedAnnotations);
        }
    }
}
