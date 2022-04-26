using System;

namespace Db.V1.Models
{
    /// <summary>
    /// ScoreSummary の DynamoDB のモデル
    /// </summary>
    public class ScoreSummaryDb
    {
        /// <summary>
        /// owner id
        /// </summary>
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// 楽譜の保存数
        /// </summary>
        public int? ScoreCount { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        public DateTimeOffset? CreateAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        public DateTimeOffset? UpdateAt { get; set; }
    }
}
