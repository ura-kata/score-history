using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DatabaseScoreDataAnnotationV1
    {
        [JsonPropertyName(ScoreDatabasePropertyNames.AnnotationsId)]
        public long Id { get; set; }

        [JsonPropertyName(ScoreDatabasePropertyNames.AnnotationsContentHash)]
        public string ContentHash { get; set; }
    }
}
