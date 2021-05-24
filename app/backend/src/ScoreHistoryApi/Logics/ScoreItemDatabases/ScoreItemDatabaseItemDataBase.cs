using System;

namespace ScoreHistoryApi.Logics.ScoreItemDatabases
{
    public abstract class ScoreItemDatabaseItemDataBase
    {
        public Guid OwnerId { get; set; }
        public Guid ScoreId { get; set; }
        public Guid ItemId { get; set; }
        public string ObjName { get; set; }
        public long Size { get; set; }
        public long TotalSize { get; set; }
    }
}
