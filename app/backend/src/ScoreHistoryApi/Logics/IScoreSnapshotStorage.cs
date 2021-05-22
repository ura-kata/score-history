using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreDatabases;
using ScoreHistoryApi.Logics.ScoreObjectStorages;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜のスナップショットを保存するストレージ
    /// </summary>
    public interface IScoreSnapshotStorage
    {
        /// <summary>
        /// スナップショットデータを作成する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="snapshotDetail"></param>
        /// <param name="accessControl"></param>
        /// <returns></returns>
        public Task CreateAsync(Guid ownerId, Guid scoreId, ScoreSnapshotDetail snapshotDetail,
            ScoreObjectAccessControls accessControl);

        /// <summary>
        /// スナップショットを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="snapshotId"></param>
        /// <returns></returns>
        public Task DeleteAsync(Guid ownerId, Guid scoreId, Guid snapshotId);

        /// <summary>
        /// 楽譜のスナップショットをすべて削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <returns></returns>
        public Task DeleteAllAsync(Guid ownerId, Guid scoreId);

        /// <summary>
        /// アクセスコントロールを設定する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="accessControl"></param>
        /// <returns></returns>
        public Task SetAccessControlPolicyAsync(Guid ownerId, Guid scoreId,
            ScoreObjectAccessControls accessControl);

        /// <summary>
        /// 楽譜のスナップショットを取得する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="snapshotId"></param>
        /// <returns></returns>
        public Task<ScoreSnapshotDetail> GetAsync(Guid ownerId, Guid scoreId, Guid snapshotId);
    }
}
