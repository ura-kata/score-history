using System;
using System.Collections.Generic;
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

        public static ScoreDetail Create(DatabaseScoreRecord scoreRecord, Dictionary<string, string> annotationSet, ScoreAccesses access)
        {
            var data = ScoreData.Create(scoreRecord.Data, annotationSet);
            return new ScoreDetail()
            {
                CreateAt = scoreRecord.CreateAt,
                UpdateAt = scoreRecord.UpdateAt,
                DataHash = scoreRecord.DataHash,
                Data = data,
                Access = access,
            };
        }
    }
}
