using System.Collections.Generic;

namespace Db.V1.Models
{
    /// <summary>
    /// 楽譜のデータの DynamoDB のモデル
    /// </summary>
    public class ScoreDataDb
    {
        /// <summary>
        /// タイトル
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 説明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// ページ枚数
        /// </summary>
        public int? PageCount { get; set; }

        /// <summary>
        /// 次のページの id
        /// </summary>
        public int? NextPageId { get; set; }

        /// <summary>
        /// ページ
        /// </summary>
        public List<PageDb>? Pages { get; set; }

        /// <summary>
        /// アノテーションの数
        /// </summary>
        public int? AnnotationCount { get; set; }

        /// <summary>
        /// 次のアノテーションの id
        /// </summary>
        public int? NextAnnotationId { get; set; }

        /// <summary>
        /// アノテーション
        /// </summary>
        public List<AnnotationRefDb>? Annotations { get; set; }
    }
}
