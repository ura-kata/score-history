using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DatabaseScoreDataAnnotationV1
    {
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.Id)]
        public long Id { get; set; }

        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.ContentHash)]
        public string ContentHash { get; set; }
    }
}
