using System;
using System.Collections.Generic;

namespace ScoreHistoryApi.Logics.ScoreObjectStorages
{
    /// <summary>
    /// 保存されたアイテムデータ
    /// </summary>
    public class SavedItemData
    {
        /// <summary> Owner id </summary>
        public Guid OwnerId { get; set; }

        /// <summary> Score id </summary>
        public Guid ScoreId { get; set; }

        /// <summary> Item id </summary>
        public Guid ItemId { get; set; }

        /// <summary> オリジナルのファイル名 </summary>
        public string OriginName { get; set; }

        /// <summary> S3 のオブジェクト名 </summary>
        public string ObjectName { get; set; }

        /// <summary> アイテムタイプ </summary>
        public ItemTypes Type { get; set; }

        /// <summary> 追加情報 </summary>
        public ExtraBase Extra { get; set; }

        /// <summary> Data </summary>
        public IReadOnlyList<byte> Data { get; set; }

        /// <summary> アクセスコントロール </summary>
        public ScoreObjectAccessControls AccessControl { get; set; }
    }
}
