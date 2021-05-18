using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 楽譜のページデータ
    /// </summary>
    public class ScorePage
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }

        [JsonPropertyName("page")]
        public string Page { get; set; }
    }
}
