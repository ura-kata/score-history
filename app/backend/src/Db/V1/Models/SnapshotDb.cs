using System;

namespace Db.V1.Models
{
    /// <summary>
    ///　スナップショットの DynamoDB のモデル
    /// </summary>
    public class SnapshotDb
    {
        /// <summary>
        /// スナップショットの ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// スナップショットの名前
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 作成日時
        /// </summary>
        public DateTimeOffset CreateAt { get; set; }
    }
}
