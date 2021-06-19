using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// 楽譜アイテム情報のベースクラス
    /// </summary>
    public abstract record ScoreItemInfoBase
    {
        /// <summary> 楽譜の ID </summary>
        [JsonPropertyName("scoreId")]
        public Guid ScoreId { get; set; }

        /// <summary> アイテムの ID </summary>
        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }

        /// <summary> アイテムのオブジェクト名 </summary>
        [JsonPropertyName("objectName")]
        public string ObjectName { get; set; }

        /// <summary> データサイズ </summary>
        [JsonPropertyName("size")]
        public long Size { get; set; }

        /// <summary> データ総サイズ </summary>
        [JsonPropertyName("totalSize")]
        public long TotalSize { get; set; }
    }
}
