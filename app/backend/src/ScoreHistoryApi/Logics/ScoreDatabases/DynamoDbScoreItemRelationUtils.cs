using System;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public static class DynamoDbScoreItemRelationUtils
    {

        /// <summary>
        /// 楽譜のアイテムの関連データのパーティションキー
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public static string ConvertToPartitionKey(Guid ownerId) => DynamoDbScoreItemRelationConstant.PartitionKeyPrefix +
                                                                    ScoreDatabaseUtils.ConvertToBase64(ownerId);
    }
}
