using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// 削除する楽譜のアイテムデータ
    /// </summary>
    public class DeletingScoreItems
    {
        [JsonPropertyName("scoreId")]
        public Guid ScoreId { get; set; }

        [JsonPropertyName("itemIds")]
        public List<Guid> ItemIds { get; set; }
    }
}
