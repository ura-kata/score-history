using System;
using System.Collections.Generic;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public static class DynamoDbScoreDataBaseExtensions
    {
        public static string GetDescriptionHash( this DynamoDbScoreDataBase self)
        {
            if (self is DynamoDbScoreDataV1 dataV1)
            {
                return dataV1.DescriptionHash;
            }

            throw new ArgumentException();
        }

        public static List<DynamoDbScorePageV1> GetPages( this DynamoDbScoreDataBase self)
        {
            if (self is DynamoDbScoreDataV1 dataV1)
            {
                return dataV1.Page;
            }

            throw new ArgumentException();
        }
    }
}
