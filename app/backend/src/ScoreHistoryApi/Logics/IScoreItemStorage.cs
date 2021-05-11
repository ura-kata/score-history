using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreObjectStorages;

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜オブジェクトのストレージ
    /// </summary>
    public interface IScoreItemStorage
    {
        /// <summary>
        /// 楽譜のオブジェクトデータを保存する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="data"></param>
        /// <param name="accessControl"></param>
        /// <returns></returns>
        Task<SavedItemData> SaveObjectAsync(Guid ownerId, Guid scoreId, byte[] data,
            ScoreObjectAccessControls accessControl);

        /// <summary>
        /// 指定した楽譜のオブジェクトデータを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="dataId"></param>
        Task DeleteObjectAsync(Guid ownerId, Guid scoreId, Guid dataId);

        /// <summary>
        /// 指定した楽譜のオブジェクトデータを全て削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        Task DeleteAllScoreObjectAsync(Guid ownerId, Guid scoreId);

        /// <summary>
        /// 指定した owner のオブジェクトデータを全て削除する
        /// </summary>
        /// <param name="ownerId"></param>
        Task DeleteAllOwnerObjectAsync(Guid ownerId);

        /// <summary>
        /// 指定した楽譜のオブジェクトのアクセスコントロールを設定する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="accessControls"></param>
        Task SetAccessControlPolicyAsync(Guid ownerId, Guid scoreId, ScoreObjectAccessControls accessControls);
    }
}
