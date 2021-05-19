using Amazon.DynamoDBv2.Model;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    /// <summary>
    /// DynamoDB のアイテムに含まれるデータのベースクラス
    /// </summary>
    public abstract class DynamoDbScoreDataBase
    {
        /// <summary>
        /// <see cref="DynamoDbScore.DataHash"/> に格納するハッシュ値を計算する
        /// </summary>
        /// <returns></returns>
        public abstract string CalcDataHash();

        /// <summary>
        /// 構造クラスから DynamoDB の <see cref="AttributeValue"/> に変換する
        /// </summary>
        /// <returns></returns>
        public abstract AttributeValue ConvertToAttributeValue();
    }
}
