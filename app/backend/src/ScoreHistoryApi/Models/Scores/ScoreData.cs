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

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("pages")]
        public ScorePage[] Pages { get; set; }

        [JsonPropertyName("annotations")]
        public ScoreAnnotation[] Annotations { get; set; }

        [JsonPropertyName("annotationDataSet")]
        public Dictionary<string, string> AnnotationDataSet { get; set; }

        public static ScoreData Create( DatabaseScoreDataV1 data, Dictionary<string,string> annotationDataSet)
        {
            return new ScoreData()
            {
                Title = data.Title,
                Description = data.Description,
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
                AnnotationDataSet = annotationDataSet.ToDictionary(x => x.Key, x => x.Value),
            };
        }
    }
}
