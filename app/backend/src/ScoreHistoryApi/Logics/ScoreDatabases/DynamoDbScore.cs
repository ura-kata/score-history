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
        public DynamoDbScoreTypes Type { get; }

        /// <summary>
        /// Owner の ID
        /// Guid の base64
        /// </summary>
        public string OwnerId { get; }

        /// <summary>
        /// Owner の ID
        /// Guid の base64
        /// </summary>
        public string ScoreId { get; }

        public string DataHash { get; }

        public string CreateAt { get; }

        public string UpdateAt { get; }

        public string Access { get; }

        public int SnapshotCount { get; }

        public DynamoDbScoreDataBase Data { get; }

        /// <summary>
        /// Snapshot の ID
        /// Guid の base64
        /// </summary>
        public string SnapshotId { get; }

        /// <summary>
        /// Snapshot の名前
        /// </summary>
        public string SnapshotName { get; }


        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public DynamoDbScore(Dictionary<string, AttributeValue> item)
        {
            OwnerId = item[DynamoDbScorePropertyNames.OwnerId].S;

            var scoreId = item[DynamoDbScorePropertyNames.ScoreId].S;

            if (scoreId == ScoreDatabaseConstant.ScoreIdSummary)
            {
                Type = DynamoDbScoreTypes.Summary;
                ScoreId = scoreId;
                return;
            }

            if (scoreId.StartsWith(ScoreDatabaseConstant.ScoreIdMainPrefix))
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

                return;
            }

            if (scoreId.StartsWith(ScoreDatabaseConstant.ScoreIdSnapPrefix))
            {
                Type = DynamoDbScoreTypes.Snapshot;

                var score = scoreId.Substring(ScoreDatabaseConstant.ScoreIdSnapPrefix.Length);
                ScoreId = score.Substring(0, 24);
                SnapshotId = score.Substring(24);
                CreateAt = item[DynamoDbScorePropertyNames.CreateAt].S;
                UpdateAt = item[DynamoDbScorePropertyNames.UpdateAt].S;
                SnapshotName = item[DynamoDbScorePropertyNames.SnapshotName].S;

                return;
            }

            throw new ArgumentException();
        }
    }
}
