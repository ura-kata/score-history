using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.ScoreItems
{
    /// <summary>
    /// 楽譜アイテムの情報
    /// </summary>
    public class ScoreImageItemInfo: ScoreItemInfoBase
    {
        /// <summary> オリジナルのファイル名 </summary>
        [BindProperty(Name = "originalName")]
        public string OriginalName { get; set; }

        /// <summary> サムネイルのオブジェクト名 </summary>
        [BindProperty(Name = "thumbnail")]
        public string Thumbnail { get; set; }

        /// <summary> サムネイルのオブジェクトサイズ </summary>
        [BindProperty(Name = "thumbnailSize")]
        public long ThumbnailSize { get; set; }
    }
}
