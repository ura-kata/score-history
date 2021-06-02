using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 新しい楽譜のページ
    /// </summary>
    public class NewScorePage
    {
        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }
        [JsonPropertyName("objectName")]
        public string ObjectName { get; set; }
        [JsonPropertyName("page")]
        public string Page { get; set; }
    }
}
