namespace Db.V1.Models
{
    /// <summary>
    /// アノテーションの参照の DynamoDB のモデル
    /// </summary>
    public class AnnotationRefDb
    {
        /// <summary>
        /// アノテーションの id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// アノテーションデータとの関連 id
        /// </summary>
        public int RefId { get; set; }

        /// <summary>
        /// アノテーションの文字列の長さ
        /// </summary>
        public int Length { get; set; }
    }
}
