using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreItemDatabases;
using ScoreHistoryApi.Models.ScoreItems;

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜アイテムデータベース
    /// </summary>
    public interface IScoreItemDatabase
    {
        /// <summary>
        /// 楽譜のアイテムを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task DeleteAsync(Guid ownerId, Guid scoreId, Guid itemId);


        /// <summary>
        /// 楽譜のアイテムを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        Task DeleteOwnerItemsAsync(Guid ownerId);

        /// <summary>
        /// 楽譜のアイテムを取得する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task<ScoreItemDatabaseItemDataBase> GetItemAsync(Guid ownerId, Guid scoreId, Guid itemId);

    }
}
