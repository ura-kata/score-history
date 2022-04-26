using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using Db.V1.Diagnostics;
using Db.V1.Models;
using Db.V1.Names;

namespace Db.V1
{
    public interface IConverter
    {
        ScoreSummaryDb ConvertToScoreSummaryDb(Dictionary<string, AttributeValue> value);
        ScoreMainDb ConvertToScoreMainDb(Dictionary<string, AttributeValue> value);
        AnnotationDataDb ConvertToAnnotationDataDb(Dictionary<string, AttributeValue> value);
        ItemSummaryDb ConvertToItemSummaryDb(Dictionary<string, AttributeValue> value);
        ItemMainDb ConvertToItemMainDb(Dictionary<string, AttributeValue> value);
    }

    /// <summary>
    /// 変換
    /// </summary>
    public class Converter:IConverter
    {
        private readonly IUtility _utility;

        public Converter(IUtility utility)
        {
            _utility = utility;
        }

        public ScoreSummaryDb ConvertToScoreSummaryDb(Dictionary<string, AttributeValue> value)
        {
            return new ScoreSummaryDb()
            {
                OwnerId = _utility.ConvertFromDbId(value[ScoreSummaryPn.PartitionKey].S.Substring(3)),
                ScoreCount = int.Parse(value[ScoreSummaryPn.ScoreCount].N),
                CreateAt = _utility.ConvertFromDbTime(value[ScoreSummaryPn.CreateAt].N),
                UpdateAt = _utility.ConvertFromDbTime(value[ScoreSummaryPn.UpdateAt].N),
            };
        }

        public ScoreMainDb ConvertToScoreMainDb(Dictionary<string, AttributeValue> value)
        {
            return new ScoreMainDb()
            {
                OwnerId = _utility.ConvertFromDbId(value[ScoreMainPn.PartitionKey].S.Substring(3)),
                ScoreId = _utility.ConvertFromDbId(value[ScoreMainPn.SortKey].S),
                CreateAt = _utility.ConvertFromDbTime(value[ScoreMainPn.CreateAt].N),
                UpdateAt = _utility.ConvertFromDbTime(value[ScoreMainPn.UpdateAt].N),
                Access = value[ScoreMainPn.Access].S,
                ETag = _utility.ConvertFromDbId(value[ScoreMainPn.ETag].S),
                TransactionStart = _utility.ConvertFromDbTime(value[ScoreMainPn.TransactionStart].N),
                TransactionTimeout = _utility.ConvertFromDbTime(value[ScoreMainPn.TransactionTimeout].S),
                SnapshotCount = int.Parse(value[ScoreMainPn.SnapshotCount].N),
                Snapshots = value[ScoreMainPn.Snapshot].L.Select(x => ConvertToSnapshotDb(x.M)).ToList(),
                ScoreData = ConvertToScoreDataDb(value[ScoreMainPn.Data].M)
            };
        }

        public SnapshotDb ConvertToSnapshotDb(Dictionary<string, AttributeValue> value)
        {
            return new SnapshotDb()
            {
                Id = _utility.ConvertFromDbId(value[ScoreMainPn.SnapshotPn.Id].S),
                Name = value[ScoreMainPn.SnapshotPn.Name].S,
                CreateAt = _utility.ConvertFromDbTime(value[ScoreMainPn.SnapshotPn.CreateAt].N)
            };
        }

        public ScoreDataDb ConvertToScoreDataDb(Dictionary<string, AttributeValue> value)
        {
            return new ScoreDataDb()
            {
                Title = value[ScoreMainPn.DataPn.Title].S,
                Description = value[ScoreMainPn.DataPn.Description].S,
                PageCount = int.Parse(value[ScoreMainPn.DataPn.PageCount].N),
                NextPageId = int.Parse(value[ScoreMainPn.DataPn.NextPageId].N),
                Pages = value[ScoreMainPn.DataPn.Page].L.Select(x => ConvertToPageDb(x.M)).ToList(),
                AnnotationCount = int.Parse(value[ScoreMainPn.DataPn.AnnotationCount].N),
                NextAnnotationId = int.Parse(value[ScoreMainPn.DataPn.NextAnnotationId].N),
                Annotations = value[ScoreMainPn.DataPn.Annotation].L.Select(x=>ConvertToAnnotationRefDb(x.M)).ToList()
            };
        }

        public PageDb ConvertToPageDb(Dictionary<string, AttributeValue> value)
        {
            return new PageDb()
            {
                Id = int.Parse(value[ScoreMainPn.DataPn.PagePn.Id].N),
                ItemId = _utility.ConvertFromDbId(value[ScoreMainPn.DataPn.PagePn.ItemId].S),
                Kind = value[ScoreMainPn.DataPn.PagePn.Kind].S,
                Name = value[ScoreMainPn.DataPn.PagePn.Name].S
            };
        }

        public AnnotationRefDb ConvertToAnnotationRefDb(Dictionary<string, AttributeValue> value)
        {
            return new AnnotationRefDb()
            {
                Id = int.Parse(value[ScoreMainPn.DataPn.AnnotationPn.Id].N),
                RefId = int.Parse(value[ScoreMainPn.DataPn.AnnotationPn.RefId].N),
                Length = int.Parse(value[ScoreMainPn.DataPn.AnnotationPn.Length].N)
            };
        }

        public AnnotationDataDb ConvertToAnnotationDataDb(Dictionary<string, AttributeValue> value)
        {
            var scoreItem = value[AnnotationDataPn.SortKey].S.Split(new[] { ":a:" }, StringSplitOptions.None);
            return new AnnotationDataDb()
            {
                OwnerId = _utility.ConvertFromDbId(value[AnnotationDataPn.PartitionKey].S.Substring(3)),
                ScoreId = _utility.ConvertFromDbId(scoreItem[0]),
                Chunk = int.Parse(scoreItem[1]),
                AnnotationTexts = value[AnnotationDataPn.Annotation].M.ToDictionary(x=>int.Parse(x.Key),x=>x.Value.S)
            };
        }

        public ItemSummaryDb ConvertToItemSummaryDb(Dictionary<string, AttributeValue> value)
        {
            return new ItemSummaryDb()
            {
                OwnerId = _utility.ConvertFromDbId(value[ItemSummaryPn.PartitionKey].S.Substring(3)),
                TotalSize = int.Parse(value[ItemSummaryPn.TotalSize].N),
                TotalCount = int.Parse(value[ItemSummaryPn.TotalCount].N),
                CreateAt = _utility.ConvertFromDbTime(value[ItemSummaryPn.CreateAt].N),
                UpdateAt = _utility.ConvertFromDbTime(value[ItemSummaryPn.UpdateAt].N)
            };
        }

        public ItemMainDb ConvertToItemMainDb(Dictionary<string, AttributeValue> value)
        {
            return new ItemMainDb()
            {
                OwnerId = _utility.ConvertFromDbId(value[ItemMainPn.PartitionKey].S.Substring(3)),
                ScoreId = _utility.ConvertFromDbId(value[ItemMainPn.SortKey].S),
                CreateAt = _utility.ConvertFromDbTime(value[ItemMainPn.CreateAt].N),
                UpdateAt = _utility.ConvertFromDbTime(value[ItemMainPn.UpdateAt].N),
                TransactionStart = _utility.ConvertFromDbTime(value[ItemMainPn.TransactionStart].N),
                TransactionTimeout = _utility.ConvertFromDbTime(value[ItemMainPn.TransactionTimeout].N),
                TotalSizeInScore = int.Parse(value[ItemMainPn.TotalSizeInScore].N),
                TotalCountInScore = int.Parse(value[ItemMainPn.TotalCountInScore].N),
                Items = value[ItemMainPn.Items].L.Select(x=>ConvertToItemDb(x.M)).ToList()
            };
        }

        public ItemDb ConvertToItemDb(Dictionary<string, AttributeValue> value)
        {
            return new ItemDb()
            {
                Id = _utility.ConvertFromDbId(value[ItemMainPn.ItemPn.Id].S),
                Kind = value[ItemMainPn.ItemPn.Kind].S,
                Size = int.Parse(value[ItemMainPn.ItemPn.Size].N),
                OriginalName = value[ItemMainPn.ItemPn.OriginName].S
            };
        }
    }
}
