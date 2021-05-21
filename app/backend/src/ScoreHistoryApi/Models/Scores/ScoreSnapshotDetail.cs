using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 楽譜のスナップショットデータ
    /// </summary>
    public class ScoreSnapshotDetail
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("data")]
        public ScoreData Data { get; set; }

        [JsonPropertyName("hashSet")]
        public Dictionary<string, string> HashSet { get; set; }


        public static ScoreSnapshotDetail Create(Guid snapshotId, string snapshotName, DynamoDbScore score, Dictionary<string, string> hashSet)
        {
            if (score.Type != DynamoDbScoreTypes.Main)
            {
                throw new ArgumentException(nameof(score));
            }

            var data = ScoreData.Create(score.Data);
            return new ScoreSnapshotDetail()
            {
                Id = snapshotId,
                Name = snapshotName,
                Data = data,
                HashSet = hashSet.ToDictionary(x=>x.Key, x=>x.Value),
            };
        }
    }
}
