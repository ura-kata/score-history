using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DatabaseScoreDataAnnotationV1
    {
        [JsonPropertyName(ScoreDatabasePropertyNames.AnnotationsId)]
        public long Id { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.AnnotationsContent)]
        public string Content { get; set; }
    }
}
