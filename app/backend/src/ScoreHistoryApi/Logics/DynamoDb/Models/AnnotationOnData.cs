namespace ScoreHistoryApi.Logics.DynamoDb.Models
{
    public record AnnotationOnData
    {
        public long Id { get; set; }
        public string Content { get; set; }
    }
}
