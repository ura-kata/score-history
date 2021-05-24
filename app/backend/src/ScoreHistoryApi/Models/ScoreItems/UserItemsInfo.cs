using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// ユーザーの楽譜のアイテム情報
    /// </summary>
    public class UserItemsInfo
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
