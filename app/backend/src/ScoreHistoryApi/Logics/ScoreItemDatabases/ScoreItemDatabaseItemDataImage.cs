namespace ScoreHistoryApi.Logics.ScoreItemDatabases
{
    public class ScoreItemDatabaseItemDataImage: ScoreItemDatabaseItemDataBase
    {
        public string OrgName { get; set; }
        public ScoreItemDatabaseItemDataImageThumbnail Thumbnail { get; set; }
    }
}
