using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// ユーザーの楽譜のアイテム情報
    /// </summary>
    public class UserItemInfo
    {
        [JsonPropertyName("itemInfos")]
        public ScoreItemInfo[] ItemInfos { get; set; }

        [JsonPropertyName("totalSize")]
        public long TotalSize { get; set; }
    }
}
