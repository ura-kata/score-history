using System;
using System.Collections.Generic;

namespace Db.V1.Models
{
    public class ScoreMainDb
    {
        /// <summary>
        /// owner id
        /// </summary>
        public Guid? OwnerId { get; set; }

        /// <summary>
        /// 楽譜の id
        /// </summary>
        public  Guid? ScoreId { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        public DateTimeOffset? CreateAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        public DateTimeOffset? UpdateAt { get; set; }

        /// <summary>
        /// アクセス
        /// </summary>
        public string? Access { get; set; }

        /// <summary>
        /// 変更検知用のタグ
        /// </summary>
        public Guid? ETag { get; set; }

        /// <summary>
        /// トランザクションの開始時間
        /// </summary>
        public DateTimeOffset? TransactionStart { get; set; }

        /// <summary>
        /// トランザクションのタイムアウト
        /// </summary>
        public DateTimeOffset? TransactionTimeout { get; set; }

        /// <summary>
        /// スナップショット数
        /// </summary>
        public int? SnapshotCount { get; set; }

        /// <summary>
        /// スナップショット
        /// </summary>
        public List<SnapshotDb>? Snapshots { get; set; }

        /// <summary>
        /// 楽譜のデータ
        /// </summary>
        public ScoreDataDb? ScoreData { get; set; }
    }
}
