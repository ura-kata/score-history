using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Db.V1.Definitions;
using Db.V1.Diagnostics;
using Db.V1.Models;
using Db.V1.Names;

namespace Db.V1.Requests
{
    public interface IRequestFactory
    {
        /// <summary>
        /// 新しい ScoreSummary を登録するリクエストの作成
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        PutItemRequest CreateNewScoreSummaryRequest(ScoreSummaryDb model);

        /// <summary>
        /// 新しい ItemSummary を登録するリクエストの作成
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        PutItemRequest CreateNewItemSummaryRequest(ItemSummaryDb model);
    }

    public class RequestFactory:IRequestFactory
    {
        private readonly IDbConfig _dbConfig;
        private readonly IUtility _utility;

        public RequestFactory(IDbConfig dbConfig, IUtility utility)
        {
            _dbConfig = dbConfig;
            _utility = utility;
        }

        public PutItemRequest CreateNewScoreSummaryRequest(ScoreSummaryDb model)
        {
            if (!model.OwnerId.HasValue)
                throw new ArgumentException(nameof(model.OwnerId));
            if (!model.CreateAt.HasValue)
                throw new ArgumentException(nameof(model.CreateAt));
            if (!model.UpdateAt.HasValue)
                throw new ArgumentException(nameof(model.UpdateAt));

            var partitionKey = PartitionPrefix.Score + _utility.ConvertIdToDbId(model.OwnerId.Value);
            var scoreCount = model.ScoreCount.HasValue ? model.ScoreCount.Value.ToString() : "0";
            var createAt = _utility.ConvertToDbTime(model.CreateAt.Value);
            var updateAt = _utility.ConvertToDbTime(model.UpdateAt.Value);

            return new PutItemRequest()
            {
                Item = new Dictionary<string, AttributeValue>()
                {
                    [ScoreSummaryPn.PartitionKey] = new(partitionKey),
                    [ScoreSummaryPn.SortKey] = new(DbConstant.SummarySortKey),
                    [ScoreSummaryPn.ScoreCount] = new(){N = scoreCount},
                    [ScoreSummaryPn.CreateAt] = new(){N = createAt},
                    [ScoreSummaryPn.UpdateAt] = new(){N = updateAt}
                },
                TableName = _dbConfig.TableName,
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    ["#o"] = ScoreSummaryPn.PartitionKey
                },
                ConditionExpression = "attribute_not_exists(#o)",
            };
        }

        public PutItemRequest CreateNewItemSummaryRequest(ItemSummaryDb model)
        {
            if (!model.OwnerId.HasValue)
                throw new ArgumentException(nameof(model.OwnerId));
            if (!model.CreateAt.HasValue)
                throw new ArgumentException(nameof(model.CreateAt));
            if (!model.UpdateAt.HasValue)
                throw new ArgumentException(nameof(model.UpdateAt));

            var partitionKey = PartitionPrefix.Item + _utility.ConvertIdToDbId(model.OwnerId.Value);
            var totalSize = model.TotalSize.HasValue ? model.TotalSize.Value.ToString() : "0";
            var totalCount = model.TotalCount.HasValue ? model.TotalCount.Value.ToString() : "0";
            var createAt = _utility.ConvertToDbTime(model.CreateAt.Value);
            var updateAt = _utility.ConvertToDbTime(model.UpdateAt.Value);

            return new PutItemRequest()
            {
                Item = new Dictionary<string, AttributeValue>()
                {
                    [ItemSummaryPn.PartitionKey] = new(partitionKey),
                    [ItemSummaryPn.SortKey] = new(DbConstant.SummarySortKey),
                    [ItemSummaryPn.TotalSize] = new(){N = totalSize},
                    [ItemSummaryPn.TotalCount] = new(){N = totalCount},
                    [ItemSummaryPn.CreateAt] = new(){N = createAt},
                    [ItemSummaryPn.UpdateAt] = new(){N = updateAt}
                },
                TableName = _dbConfig.TableName,
                ExpressionAttributeNames = new Dictionary<string, string>()
                {
                    ["#o"] = ItemSummaryPn.PartitionKey
                },
                ConditionExpression = "attribute_not_exists(#o)",
            };
        }
    }
}
