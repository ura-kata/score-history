using System;

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
    }
}
