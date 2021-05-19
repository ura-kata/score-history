using System.Text.Json.Serialization;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public class DynamoDbScorePageV1
    {
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Id)]
        public long Id { get; set; }
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.ItemId)]
        public string ItemId { get; set; }
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Page)]
        public string Page { get; set; }
    }
}
