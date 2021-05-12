using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Logics.ScoreItemDatabases
{
    public static class ScoreItemDatabaseUtils
    {
        public static (Dictionary<string, AttributeValue> items, string owner) CreateDynamoDbValue(ScoreItemDatabaseItemDataBase itemData, DateTimeOffset now)
        {
            var items = new Dictionary<string, AttributeValue>();

            var owner = ScoreDatabaseUtils.ConvertToBase64(itemData.OwnerId);
            var score = ScoreDatabaseUtils.ConvertToBase64(itemData.ScoreId);
            var item = ScoreDatabaseUtils.ConvertToBase64(itemData.ItemId);
            var at = ScoreDatabaseUtils.ConvertToUnixTimeMilli(now);

            items[ScoreItemDatabasePropertyNames.OwnerId] = new AttributeValue(owner);
            items[ScoreItemDatabasePropertyNames.ItemId] = new AttributeValue(score + item);
            items[ScoreItemDatabasePropertyNames.ObjName] = new AttributeValue(itemData.ObjName);
            items[ScoreItemDatabasePropertyNames.Size] = new AttributeValue() {N = itemData.Size.ToString()};
            items[ScoreItemDatabasePropertyNames.At] = new AttributeValue(at);

            if ( itemData is ScoreItemDatabaseItemDataImage itemDataImage)
            {
                items[ScoreItemDatabasePropertyNames.Type] = new AttributeValue(ScoreItemDatabaseConstant.TypeImage);

                items[ScoreItemDatabasePropertyNames.OrgName] = new AttributeValue(itemDataImage.OrgName);

                items[ScoreItemDatabasePropertyNames.Thumbnail] = new AttributeValue()
                {
                    M = new Dictionary<string, AttributeValue>()
                    {
                        [ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.ObjName] =
                            new AttributeValue(itemDataImage.Thumbnail.ObjName),
                        [ScoreItemDatabasePropertyNames.ThumbnailPropertyNames.Size] = new AttributeValue()
                            {N = itemDataImage.Thumbnail.Size.ToString()},
                    }
                };
            }

            return (items, owner);
        }

        public static long GetSize(ScoreItemDatabaseItemDataBase itemData)
        {
            long size = itemData.Size;

            if (itemData is ScoreItemDatabaseItemDataImage itemDataImage)
            {
                size += itemDataImage.Thumbnail.Size;
            }

            return size;
        }
    }
}
