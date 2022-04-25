using System;
using System.Collections.Generic;

namespace Db.V1.Models
{
    public class ItemMainDb
    {
        /// <summary>
        /// owner id
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// 楽譜の id
        /// </summary>
        public Guid ScoreId { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        public DateTimeOffset CreateAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        public DateTimeOffset UpdateAt { get; set; }

        /// <summary>
        /// トランザクションの開始日時
        /// </summary>
        public DateTimeOffset TransactionStart { get; set; }

        /// <summary>
        /// トランザクションのタイムアウト
        /// </summary>
        public DateTimeOffset TransactionTimeout { get; set; }

        /// <summary>
        /// 楽譜に含まれるアイテムのトータルサイズ
        /// </summary>
        public int TotalSizeInScore { get; set; }

        /// <summary>
        /// 楽譜に含まれるアイテムの数
        /// </summary>
        public int TotalCountInScore { get; set; }

        /// <summary>
        /// アイテム
        /// </summary>
        public List<ItemDb> Items { get; set; }
    }
}
