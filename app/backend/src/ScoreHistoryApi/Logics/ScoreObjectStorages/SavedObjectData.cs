using System;
using System.Collections.Generic;

namespace ScoreHistoryApi.Logics.ScoreObjectStorages
{
    public class SavedObjectData
    {
        /// <summary> Owner id </summary>
        public Guid OwnerId { get; set; }

        /// <summary> Score id </summary>
        public Guid ScoreId { get; set; }

        /// <summary> Data id </summary>
        public Guid Id { get; set; }

        /// <summary> Data </summary>
        public IReadOnlyList<byte> Data { get; set; }
    }
}
