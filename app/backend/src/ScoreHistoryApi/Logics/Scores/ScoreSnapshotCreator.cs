using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreObjectStorages;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreSnapshotCreator
    {
        private readonly IScoreDatabase _scoreDatabase;
        private readonly IScoreSnapshotStorage _scoreSnapshotStorage;

        public ScoreSnapshotCreator(IScoreDatabase scoreDatabase, IScoreSnapshotStorage scoreSnapshotStorage)
        {
            _scoreDatabase = scoreDatabase;
            _scoreSnapshotStorage = scoreSnapshotStorage;
        }

        public async Task CreateAsync(Guid ownerId, Guid scoreId, string snapshotName)
        {
            if (snapshotName is null)
            {
                throw new ArgumentNullException(nameof(snapshotName));
            }

            var trimSnapshotName = snapshotName.Trim();

            if (string.IsNullOrWhiteSpace(trimSnapshotName))
            {
                throw new ArgumentException(nameof(snapshotName));
            }

            var snapshot = await _scoreDatabase.CreateSnapshotAsync(ownerId, scoreId, trimSnapshotName);

            var access = snapshot.access == ScoreAccesses.Public
                ? ScoreObjectAccessControls.Public
                : ScoreObjectAccessControls.Private;
            await _scoreSnapshotStorage.CreateAsync(ownerId, scoreId, snapshot.snapshot, access);
        }
    }
}
