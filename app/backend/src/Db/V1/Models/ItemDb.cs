using System;

namespace Db.V1.Models
{
    public class ItemDb
    {
        /// <summary>
        /// item id
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// アイテムオブジェクトの種類
        /// </summary>
        public string? Kind { get; set; }

        /// <summary>
        /// アイテムのサイズ
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// アイテムのオリジナル名
        /// </summary>
        public string? OriginalName { get; set; }
    }
}
