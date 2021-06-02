using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.ScoreItemDatabases
{
    public static class ScoreItemDatabaseUtils
    {
        /// <summary>
        /// DynamoDB のアイテムデータを作成する
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        public static (Dictionary<string, AttributeValue> items, string partitionKey, string score, string item, long totalSize)
            CreateDynamoDbValue(ScoreItemDatabaseItemDataBase itemData, DateTimeOffset now)
        {
            var items = new Dictionary<string, AttributeValue>();

            var partitionKey = ScoreItemDatabaseUtils.ConvertToPartitionKey(itemData.OwnerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(itemData.ScoreId);
            var item = ScoreDatabaseUtils.ConvertToBase64(itemData.ItemId);
            var at = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

            items[ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(partitionKey);
            items[ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(score + item);
            items[ScoreItemDatabasePropertyNames.ObjName] = new AttributeValue(itemData.ObjName);
            items[ScoreItemDatabasePropertyNames.Size] = new AttributeValue {N = itemData.Size.ToString()};
            items[ScoreItemDatabasePropertyNames.At] = new AttributeValue(at);

            var totalSize = itemData.Size;

            if (itemData is ScoreItemDatabaseItemDataImage itemDataImage)
            {
                items[ScoreItemDatabasePropertyNames.Type] = new AttributeValue(ScoreItemDatabaseConstant.TypeImage);

                items[ScoreItemDatabasePropertyNames.OrgName] = new AttributeValue(itemDataImage.OrgName);

                items[ScoreItemDatabasePropertyNames.Thumbnail] = new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        [ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.ObjName] =
                            new AttributeValue(itemDataImage.Thumbnail.ObjName),
                        [ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size] = new AttributeValue
                            {N = itemDataImage.Thumbnail.Size.ToString()}
                    }
                };

                totalSize += itemDataImage.Thumbnail.Size;
            }

            items[ScoreItemDatabasePropertyNames.TotalSize] = new AttributeValue {N = totalSize.ToString()};

            return (items, partitionKey, score, item, totalSize);
        }

        /// <summary>
        ///     DynamoDB のアイテムを変換する
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static ScoreItemDatabaseItemDataBase ConvertFromDynamoDbValue(Dictionary<string, AttributeValue> item)
        {
            var ownerIdValue = item[ScoreItemDatabasePropertyNames.OwnerId];
            var itemIdValue = item[ScoreItemDatabasePropertyNames.ItemId];
            var sizeValue = item[ScoreItemDatabasePropertyNames.Size];

            var atValue = item[ScoreItemDatabasePropertyNames.At];

            var typeValue = item[ScoreItemDatabasePropertyNames.Type];

            var objNameValue = item[ScoreItemDatabasePropertyNames.ObjName];

            var totalSizeValue = item[ScoreItemDatabasePropertyNames.TotalSize];

            ScoreItemDatabaseItemDataBase result = default;

            if (typeValue.S == ScoreItemDatabaseConstant.TypeImage)
            {
                var orgNameValue = item[ScoreItemDatabasePropertyNames.OrgName];
                var thumbnailValue = item[ScoreItemDatabasePropertyNames.Thumbnail];

                var thumbObjNameValue =
                    thumbnailValue.M[ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.ObjName];
                var thumbSizeValue =
                    thumbnailValue.M[ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size];

                result = new ScoreItemDatabaseItemDataImage
                {
                    OrgName = orgNameValue.S,
                    Thumbnail = new ScoreItemDatabaseItemDataImageThumbnail
                    {
                        ObjName = thumbObjNameValue.S,
                        Size = long.Parse(thumbSizeValue.N)
                    }
                };
            }
            else
            {
                throw new InvalidOperationException();
            }

            result.Size = long.Parse(sizeValue.N);
            result.TotalSize = long.Parse(totalSizeValue.N);
            result.OwnerId = ScoreItemDatabaseUtils.ConvertFromPartitionKey(ownerIdValue.S);

            var scoreBase64 = itemIdValue.S.Substring(0, ScoreItemDatabaseConstant.ScoreIdLength);
            var itemBase64 = itemIdValue.S.Substring(ScoreItemDatabaseConstant.ScoreIdLength);

            result.ScoreId = ScoreDatabaseUtils.ConvertToGuid(scoreBase64);
            result.ItemId = ScoreDatabaseUtils.ConvertToGuid(itemBase64);
            result.ObjName = objNameValue.S;

            return result;
        }

        /// <summary>
        /// 楽譜のアイテムデータのパーティションキーから UUID に変換する
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Guid ConvertFromPartitionKey(string partitionKey) =>
            ScoreDatabaseUtils.ConvertToGuid(
                partitionKey.Substring(ScoreItemDatabaseConstant.PartitionKeyPrefix.Length));

        /// <summary>
        /// 楽譜のアイテムデータのパーティションキー
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public static string ConvertToPartitionKey(Guid ownerId) => ScoreItemDatabaseConstant.PartitionKeyPrefix +
                                                                    ScoreDatabaseUtils.ConvertToBase64(ownerId);
    }
}
