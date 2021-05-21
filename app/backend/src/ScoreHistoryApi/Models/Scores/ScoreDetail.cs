using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Models.Scores
{
    public class ScoreDetail
    {
        [JsonPropertyName("createAt")]
        public DateTimeOffset CreateAt { get; set; }

        [JsonPropertyName("updateAt")]
        public DateTimeOffset UpdateAt { get; set; }

        [JsonPropertyName("data")]
        public ScoreData Data { get; set; }

        [JsonPropertyName("dataHash")]
        public string DataHash { get; set; }

        [JsonPropertyName("access")]
        public ScoreAccesses Access { get; set; }

        [JsonPropertyName("hashSet")]
        public Dictionary<string, string> HashSet { get; set; }

        public static ScoreDetail Create(DynamoDbScore score, Dictionary<string, string> hashSet)
        {
            if (score.Type != DynamoDbScoreTypes.Main)
            {
                throw new ArgumentException(nameof(score));
            }

            var data = ScoreData.Create(score.Data);
            return new ScoreDetail()
            {
                CreateAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(score.CreateAt),
                UpdateAt = ScoreDatabaseUtils.ConvertFromUnixTimeMilli(score.UpdateAt),
                DataHash = score.DataHash,
                Data = data,
                Access = ScoreDatabaseUtils.ConvertToScoreAccess(score.Access),
                HashSet = hashSet.ToDictionary(x=>x.Key, x=>x.Value),
            };
        }
    }
}
