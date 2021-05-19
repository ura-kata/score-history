using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DynamoDbScore
    {
        [JsonPropertyName(DynamoDbScorePropertyNames.OwnerId)]
        public string OwnerId { get; set; }
    }

    /// <summary>
    /// DynamoDB のアイテムに含まれるデータ
    /// </summary>
    public class DynamoDbScoreDataV1: DynamoDbScoreDataBase
    {
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.Title)]
        public string Title { get; set; }

        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash)]
        public string DescriptionHash { get; set; }

        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.DataVersion)]
        public string Version { get; set; } = ScoreDatabaseConstant.ScoreDataVersion1;

        private List<DatabaseScoreDataPageV1> _page;
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.Pages)]
        public List<DatabaseScoreDataPageV1> Page
        {
            get => _page ??= new List<DatabaseScoreDataPageV1>();
            set => _page = value;
        }

        private List<DatabaseScoreDataAnnotationV1> _annotations;
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.Annotations)]
        public List<DatabaseScoreDataAnnotationV1> Annotations
        {
            get => _annotations ??= new List<DatabaseScoreDataAnnotationV1>();
            set => _annotations = value;
        }
    }
}
