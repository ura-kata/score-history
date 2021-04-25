using System;
using ScoreHistoryApi.Logics.ScoreObjectStorages;

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜オブジェクトのストレージ
    /// </summary>
    public interface IScoreObjectStorage
    {
        /// <summary>
        /// 楽譜のオブジェクトデータを保存する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        SavedObjectData SaveObject(Guid ownerId, Guid scoreId, byte[] data);

        /// <summary>
        /// 指定した楽譜のオブジェクトデータを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="dataId"></param>
        void DeleteObject(Guid ownerId, Guid scoreId, Guid dataId);

        /// <summary>
        /// 指定した楽譜のオブジェクトデータを全て削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        void DeleteAllScoreObject(Guid ownerId, Guid scoreId);

        /// <summary>
        /// 指定した owner のオブジェクトデータを全て削除する
        /// </summary>
        /// <param name="ownerId"></param>
        void DeleteAllOwnerObject(Guid ownerId);

        /// <summary>
        /// 指定した楽譜のオブジェクトのアクセスコントロールを設定する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="accessControls"></param>
        void SetAccessControlPolicy(Guid ownerId, Guid scoreId, ScoreObjectAccessControls accessControls);
    }
}
