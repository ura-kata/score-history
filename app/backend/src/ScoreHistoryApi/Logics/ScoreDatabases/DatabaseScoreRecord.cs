using System;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DatabaseScoreRecord
    {
        public DateTimeOffset CreateAt { get; set; }
        public DateTimeOffset UpdateAt { get; set; }
        public string DataHash { get; set; }
        public DynamoDbScoreDataV1 Data { get; set; }
    }
}
