using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ScoreHistoryApi.Models.Scores;

namespace ScoreHistoryApi.Logics
{
    /// <summary>
    /// 楽譜のデータベース
    /// </summary>
    public interface IScoreDatabase
    {
        /// <summary>
        /// データベースを初期化する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        Task InitializeAsync(Guid ownerId);

        /// <summary>
        /// 楽譜の作成
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        Task CreateAsync(Guid ownerId, [NotNull] string title, [AllowNull] string description);

        /// <summary>
        /// 楽譜の削除
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        Task DeleteAsync(Guid ownerId, Guid scoreId);

        /// <summary>
        /// タイトルの更新
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="title"></param>
        Task UpdateTitleAsync(Guid ownerId, Guid scoreId, string title);

        /// <summary>
        /// 説明の更新
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="description"></param>
        Task UpdateDescriptionAsync(Guid ownerId, Guid scoreId, string description);

        /// <summary>
        /// ページの追加
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="pages"></param>
        Task AddPagesAsync(Guid ownerId, Guid scoreId, List<NewScorePage> pages);

        /// <summary>
        /// ページの削除
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="pageIds"></param>
        Task RemovePagesAsync(Guid ownerId, Guid scoreId, List<int> pageIds);

        /// <summary>
        /// ページの置き換え
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="pages"></param>
        Task ReplacePagesAsync(Guid ownerId, Guid scoreId, List<PatchScorePage> pages);

        /// <summary>
        /// アノテーションの追加
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="annotations"></param>
        Task AddAnnotationsAsync(Guid ownerId, Guid scoreId, List<NewScoreAnnotation> annotations);

        /// <summary>
        /// アノテーションの削除
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="annotationIds"></param>
        Task RemoveAnnotationsAsync(Guid ownerId, Guid scoreId, List<int> annotationIds);

        /// <summary>
        /// アノテーションの置き換え
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="annotations"></param>
        Task ReplaceAnnotationsAsync(Guid ownerId, Guid scoreId, List<PatchScoreAnnotation> annotations);

        /// <summary>
        /// 楽譜のサマリデータを一覧で取得する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        Task<IReadOnlyList<ScoreSummary>> GetScoreSummariesAsync(Guid ownerId);

        /// <summary>
        /// 楽譜の詳細データを取得する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <returns></returns>
        Task<ScoreDetail> GetScoreDetailAsync(Guid ownerId, Guid scoreId);

        /// <summary>
        /// 楽譜の詳細データを取得する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="snapshotName"></param>
        /// <returns></returns>
        Task<ScoreDetail> GetScoreDetailAsync(Guid ownerId, Guid scoreId, string snapshotName);

        /// <summary>
        /// スナップショットを作成する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="snapshotName"></param>
        Task CreateSnapshotAsync(Guid ownerId, Guid scoreId, string snapshotName);

        /// <summary>
        /// スナップショットを削除する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <param name="snapshotName"></param>
        Task DeleteSnapshotAsync(Guid ownerId, Guid scoreId, string snapshotName);

        /// <summary>
        /// スナップショットの名前一覧を取得する
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="scoreId"></param>
        /// <returns></returns>
        Task<IReadOnlyList<string>> GetSnapshotNamesAsync(Guid ownerId, Guid scoreId);

    }
}