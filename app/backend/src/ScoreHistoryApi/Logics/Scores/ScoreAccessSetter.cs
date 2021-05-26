using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics.Scores
{
    public class ScoreAccessSetter
    {
        private readonly IScoreDatabase _scoreDatabase;
        private readonly IScoreItemStorage _scoreItemStorage;
        private readonly IScoreSnapshotStorage _scoreSnapshotStorage;

        public ScoreAccessSetter(IScoreDatabase scoreDatabase, IScoreItemStorage scoreItemStorage,
            IScoreSnapshotStorage scoreSnapshotStorage)
        {
            _scoreDatabase = scoreDatabase;
            _scoreItemStorage = scoreItemStorage;
            _scoreSnapshotStorage = scoreSnapshotStorage;
        }

        public async Task SetAccessAsync(Guid ownerId, Guid scoreId, PatchScoreAccess access)
        {
            await _scoreDatabase.SetAccessAsync(ownerId, scoreId, access.Access);
            var accessControl = access.Access == ScoreAccesses.Public
                ? ScoreObjectAccessControls.Public
                : ScoreObjectAccessControls.Private;
            await _scoreItemStorage.SetAccessControlPolicyAsync(ownerId, scoreId, accessControl);
            await _scoreSnapshotStorage.SetAccessControlPolicyAsync(ownerId, scoreId, accessControl);
        }
    }
}
