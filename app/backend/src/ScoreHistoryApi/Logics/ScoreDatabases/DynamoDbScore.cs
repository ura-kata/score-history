using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.DynamoDBv2.Model;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public enum DynamoDbScoreTypes
    {
        Summary,
        Main,
        Snapshot,
    }

    /// <summary>
    /// DynamoDB の楽譜アイテムの構造
    /// </summary>
    public class DynamoDbScore
    {
        public DynamoDbScoreTypes Type { get; set; }

        /// <summary>
        /// Owner の ID
        /// Guid の base64
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Owner の ID
        /// Guid の base64
        /// </summary>
        public string ScoreId { get; set; }

        public string DataHash { get; set; }

        public string CreateAt { get; set; }

        public string UpdateAt { get; set; }

        public string Access { get; set; }

        public int SnapshotCount { get; set; }

        public DynamoDbScoreDataBase Data { get; set; }

        /// <summary>
        /// Snapshot の ID
        /// Guid の base64
        /// </summary>
        public string SnapshotId { get; set; }

        /// <summary>
        /// Snapshot の名前
        /// </summary>
        public string SnapshotName { get; set; }


        public DynamoDbScore(Dictionary<string, AttributeValue> item)
        {
            OwnerId = item[DynamoDbScorePropertyNames.OwnerId].S;

            var scoreId = item[DynamoDbScorePropertyNames.ScoreId].S;

            if (scoreId == ScoreDatabaseConstant.ScoreIdSummary)
            {
                Type = DynamoDbScoreTypes.Summary;
                ScoreId = scoreId;
            }
            else if (scoreId.StartsWith(ScoreDatabaseConstant.ScoreIdMainPrefix))
            {
                Type = DynamoDbScoreTypes.Main;

                ScoreId = scoreId.Substring(ScoreDatabaseConstant.ScoreIdMainPrefix.Length);
                DataHash = item[DynamoDbScorePropertyNames.DataHash].S;
                CreateAt = item[DynamoDbScorePropertyNames.CreateAt].S;
                UpdateAt = item[DynamoDbScorePropertyNames.UpdateAt].S;
                Access = item[DynamoDbScorePropertyNames.Access].S;
                SnapshotCount = int.Parse(item[DynamoDbScorePropertyNames.SnapshotCount].N,
                    CultureInfo.InvariantCulture);

                var dataValue = item[DynamoDbScorePropertyNames.Data];
                if (dataValue is null)
                    throw new InvalidOperationException("Data not found.");
                if(!DynamoDbScoreDataV1.TryMapFromAttributeValue(dataValue, out var data))
                    throw new InvalidOperationException("Data convert error.");
                Data = data;

            }
            else if (scoreId.StartsWith(ScoreDatabaseConstant.ScoreIdSnapPrefix))
            {
                Type = DynamoDbScoreTypes.Snapshot;

                var score = scoreId.Substring(ScoreDatabaseConstant.ScoreIdSnapPrefix.Length);
                ScoreId = score.Substring(0, 24);
                SnapshotId = score.Substring(24);
                CreateAt = item[DynamoDbScorePropertyNames.CreateAt].S;
                UpdateAt = item[DynamoDbScorePropertyNames.UpdateAt].S;
                SnapshotName = item[DynamoDbScorePropertyNames.SnapshotName].S;
            }

            throw new ArgumentException();
        }
    }
}
