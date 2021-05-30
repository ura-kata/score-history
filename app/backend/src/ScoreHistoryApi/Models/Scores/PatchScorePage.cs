using System;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 置き換える楽譜のページ
    /// </summary>
    public class PatchScorePage
    {
        [JsonPropertyName("targetPageId")]
        public long TargetPageId { get; set; }
        [JsonPropertyName("itemId")]
        public Guid ItemId { get; set; }
        [JsonPropertyName("page")]
        public string Page { get; set; }
        [JsonPropertyName("objectName")]
        public string ObjectName { get; set; }
    }
}
