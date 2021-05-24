using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// 所有者のアイテム情報
    /// </summary>
    public class OwnerItemsInfo
    {
        /// <summary>
        /// アイテム
        /// </summary>
        [JsonPropertyName("itemInfos")]
        public List<ScoreItemInfoBase> ItemInfos { get; set; }

        /// <summary>
        /// 全てのアイテムの総サイズ
        /// </summary>
        [JsonPropertyName("totalSize")]
        public long TotalSize { get; set; }
    }
}
