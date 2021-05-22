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

        public static ScoreData Create(DynamoDbScoreDataBase data)
        {
            if (data is DynamoDbScoreDataV1 dataV1)
            {
                return Create(dataV1);
            }

            throw new ArgumentException(nameof(data));
        }

        public static ScoreData Create(DynamoDbScoreDataV1 data)
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
            };
        }
    }
}
