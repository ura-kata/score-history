using System;
using System.Collections.Generic;

namespace Db.V1.Models
{
    /// <summary>
    /// アノテーションデータの DynamoDB のモデル
    /// </summary>
    public class AnnotationDataDb
    {
        /// <summary>
        /// owner id
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// 楽譜の id
        /// </summary>
        public Guid ScoreId { get; set; }

        /// <summary>
        /// チャンク番号
        /// </summary>
        public int Chunk { get; set; }

        /// <summary>
        /// アノテーションのテキストデータ
        /// Key: 関連 id, Value: テキストデータ
        /// </summary>
        public Dictionary<int,string> AnnotationTexts { get; set; }
    }
}
