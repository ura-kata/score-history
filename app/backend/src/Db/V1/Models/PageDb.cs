using System;

namespace Db.V1.Models
{
    /// <summary>
    /// ページの DynamoDB のモデル
    /// </summary>
    public class PageDb
    {
        /// <summary>
        /// ページの id
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// アイテムの id
        /// </summary>
        public Guid? ItemId { get; set; }

        /// <summary>
        /// アイテムオブジェクトの種類
        /// </summary>
        public string? Kind { get; set; }

        /// <summary>
        /// ページの名前
        /// </summary>
        public string? Name { get; set; }
    }
}
