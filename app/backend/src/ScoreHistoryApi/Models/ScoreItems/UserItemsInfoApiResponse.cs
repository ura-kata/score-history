using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// ユーザーの楽譜のアイテム情報の API のレスポンス
    /// </summary>
    public class UserItemsInfoApiResponse
    {
        public UserItemsInfoApiResponse(UserItemsInfo userItemsInfo)
        {
            ItemInfos = userItemsInfo.ItemInfos.Select(x=>(object)x).ToList();
            TotalSize = userItemsInfo.TotalSize;
        }

        /// <summary>
        /// アイテム
        /// </summary>
        [JsonPropertyName("itemInfos")]
        public List<object> ItemInfos { get; set; }

        /// <summary>
        /// 全てのアイテムの総サイズ
        /// </summary>
        [JsonPropertyName("totalSize")]
        public long TotalSize { get; set; }
    }
}
