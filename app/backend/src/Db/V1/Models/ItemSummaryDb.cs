using System;

namespace Db.V1.Models
{
    /// <summary>
    /// アイテムの DynamoDB のモデル
    /// </summary>
    public class ItemSummaryDb
    {
        /// <summary>
        /// owner id
        /// </summary>
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// owner が所有しているアイテムの合計サイズ
        /// </summary>
        public int? TotalSize { get; set; }

        /// <summary>
        /// owner が所有しているアイテムの数
        /// </summary>
        public int? TotalCount { get; set; }

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
