using System;
using System.Threading.Tasks;
using ScoreHistoryApi.Logics.ScoreItemDatabases;

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜アイテムデータベース
    /// </summary>
    public interface IScoreItemDatabase
    {
        /// <summary>
        /// データベースを初期化する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        Task InitializeAsync(Guid ownerId);

        /// <summary>
        /// 楽譜のアイテムを作成する
        /// </summary>
        /// <param name="itemData"></param>
        /// <returns></returns>
        Task CreateAsync(ScoreItemDatabaseItemDataBase itemData);

        /// <summary>
        /// 楽譜のアイテムを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        Task DeleteAsync(Guid ownerId, Guid itemId);

        /// <summary>
        /// 楽譜のアイテムを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        Task DeleteOwnerItemsAsync(Guid ownerId);
    }
}
