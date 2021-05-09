using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DatabaseScoreDataV1
    {
        [JsonPropertyName(ScoreDatabasePropertyNames.Title)]
        public string Title { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.Description)]
        public string Description { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.DataVersion)]
        public string Version { get; set; } = ScoreDatabaseConstant.ScoreDataVersion1;

        private List<DatabaseScoreDataPageV1> _page;
        [JsonPropertyName(ScoreDatabasePropertyNames.Pages)]
        public List<DatabaseScoreDataPageV1> Page
        {
            get => _page ??= new List<DatabaseScoreDataPageV1>();
            set => _page = value;
        }

        private List<DatabaseScoreDataAnnotationV1> _annotations;
        [JsonPropertyName(ScoreDatabasePropertyNames.Annotations)]
        public List<DatabaseScoreDataAnnotationV1> Annotations
        {
            get => _annotations ??= new List<DatabaseScoreDataAnnotationV1>();
            set => _annotations = value;
        }
    }
}
