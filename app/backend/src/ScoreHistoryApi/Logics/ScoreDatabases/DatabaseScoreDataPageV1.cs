using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DatabaseScoreDataPageV1
    {
        [JsonPropertyName(ScoreDatabasePropertyNames.PagesId)]
        public long Id { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.PagesItemId)]
        public string ItemId { get; set; }
        [JsonPropertyName(ScoreDatabasePropertyNames.PagesPage)]
        public string Page { get; set; }
    }
}
