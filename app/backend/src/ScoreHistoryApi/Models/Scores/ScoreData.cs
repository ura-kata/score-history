using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 楽譜データ
    /// </summary>
    public class ScoreData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("descriptionHash")]
        public string DescriptionHash { get; set; }

        [JsonPropertyName("pages")]
        public ScorePage[] Pages { get; set; }

        [JsonPropertyName("annotations")]
        public ScoreAnnotation[] Annotations { get; set; }

        [JsonPropertyName("hashSet")]
        public Dictionary<string, string> HashSet { get; set; }

        public static ScoreData Create(DynamoDbScoreDataBase data, Dictionary<string,string> hashSet)
        {
            if (data is DynamoDbScoreDataV1 dataV1)
            {
                return Create(dataV1, hashSet);
            }

            throw new ArgumentException(nameof(data));
        }

        public static ScoreData Create( DynamoDbScoreDataV1 data, Dictionary<string,string> hashSet)
        {
            return new ScoreData()
            {
                Title = data.Title,
                DescriptionHash = data.DescriptionHash,
                Pages = data.Page.Select(x => new ScorePage()
                {
                    Id = x.Id,
                    Page = x.Page,
                    ItemId = ScoreDatabaseUtils.ConvertToGuid(x.ItemId),
                }).ToArray(),
                Annotations = data.Annotations.Select(x => new ScoreAnnotation()
                {
                    Id = x.Id,
                    ContentHash = x.ContentHash,
                }).ToArray(),
                HashSet = hashSet.ToDictionary(x => x.Key, x => x.Value),
            };
        }
    }
}
