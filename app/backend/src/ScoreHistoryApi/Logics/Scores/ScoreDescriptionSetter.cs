using System;
using System.Threading.Tasks;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreDescriptionSetter
    {
        private readonly IScoreDatabase _scoreDatabase;
        private readonly IScoreQuota _scoreQuota;

        public ScoreDescriptionSetter(IScoreDatabase scoreDatabase, IScoreQuota scoreQuota)
        {
            _scoreDatabase = scoreDatabase;
            _scoreQuota = scoreQuota;
        }

        public async Task SetDescriptionAsync(Guid ownerId, Guid scoreId, string description)
        {
            if (description == null)
                throw new ArgumentNullException(nameof(description));

            var trimDescription = description.Trim();

            var titleMaxLength = _scoreQuota.DescriptionMaxLength;
            if (titleMaxLength < trimDescription.Length)
                throw new ArgumentException(nameof(description));

            await _scoreDatabase.UpdateDescriptionAsync(ownerId, scoreId, trimDescription);
        }

    }
}
