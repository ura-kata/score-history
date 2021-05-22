using System;
using System.Threading.Tasks;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreTitleSetter
    {
        private readonly IScoreDatabase _scoreDatabase;
        private readonly IScoreQuota _scoreQuota;

        public ScoreTitleSetter(IScoreDatabase scoreDatabase, IScoreQuota scoreQuota)
        {
            _scoreDatabase = scoreDatabase;
            _scoreQuota = scoreQuota;
        }

        public async Task SetTitleAsync(Guid ownerId, Guid scoreId, string title)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            var trimTitle = title.Trim();
            if (trimTitle == "")
                throw new ArgumentException(nameof(title));

            var titleMaxLength = _scoreQuota.TitleMaxLength;
            if (titleMaxLength < trimTitle.Length)
                throw new ArgumentException(nameof(title));

            await _scoreDatabase.UpdateTitleAsync(ownerId, scoreId, trimTitle);
        }

    }
}
