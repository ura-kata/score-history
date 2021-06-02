#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public static class DynamoDbScoreDataUtils
    {
        /// <summary>
        /// コンテンツデータのハッシュ値を計算する
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CalcHash(string prefix, string data)
        {
            var buffer = Encoding.UTF8.GetBytes(prefix + data);
            return Convert.ToBase64String(MD5.Create().ComputeHash(buffer));
        }


        /// <summary>
        /// 説明のハッシュ値を計算する際に元データの先頭に付加するプレフィックス
        /// </summary>
        public const string DescriptionPrefix = "desc:";

        /// <summary>
        /// アノテーションのハッシュ値を計算する際に元データの先頭に付加するプレフィックス
        /// </summary>
        public const string AnnotationPrefix = "ann:";


        /// <summary>
        /// 楽譜のアイテムデータのパーティションキー
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public static string ConvertToPartitionKey(Guid ownerId) => DynamoDbScoreDataConstant.PartitionKeyPrefix +
                                                                    ScoreDatabaseUtils.ConvertToBase64(ownerId);
    }
}
