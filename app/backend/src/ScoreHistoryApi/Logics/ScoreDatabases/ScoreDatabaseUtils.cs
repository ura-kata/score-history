using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    public static class ScoreDatabaseUtils
    {
        /// <summary>
        /// UUID を Base64 エンコードで変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string ConvertToBase64(Guid id) =>
            Convert.ToBase64String(id.ToByteArray());

        /// <summary>
        /// Base64 エンコードされた id を UUID に変換する
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Guid ConvertToGuid(string id) =>
            new Guid(Convert.FromBase64String(id));

        /// <summary>
        /// データベースのデータからハッシュ値を計算する
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CalcHash(DatabaseScoreDataV1 data)
        {
            var option = new JsonSerializerOptions()
            {
                AllowTrailingCommas = false,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = true,
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = false,
                Encoder = JavaScriptEncoder.Default,
            };
            var json = JsonSerializer.SerializeToUtf8Bytes(data, option);
            return Convert.ToBase64String(MD5.Create().ComputeHash(json));
        }

        /// <summary>
        /// DynamoDB のデータベースのデータからデータベースデータのクラスにマッピングする
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DatabaseScoreDataV1 ConvertToDatabaseScoreDataV1(AttributeValue value)
        {
            var result = new DatabaseScoreDataV1();

            foreach (var (key, v) in value.M)
            {
                switch (key)
                {
                    case ScoreDatabasePropertyNames.Title:
                    {
                        result.Title = v.S;
                        break;
                    }
                    case ScoreDatabasePropertyNames.Description:
                    {
                        result.Description = v.S;
                        break;
                    }
                    case ScoreDatabasePropertyNames.DataVersion:
                    {
                        result.Version = v.S;
                        break;
                    }
                    case ScoreDatabasePropertyNames.Pages:
                    {
                        var pages = new List<DatabaseScoreDataPageV1>();
                        if (0 < v.L.Count)
                        {
                            foreach (var pageValue in v.L)
                            {
                                if(pageValue.M.Count == 0)
                                    continue;

                                var p = new DatabaseScoreDataPageV1();
                                foreach (var (pageItemKey,pageItemValue) in pageValue.M)
                                {
                                    switch (pageItemKey)
                                    {
                                        case ScoreDatabasePropertyNames.PagesId:
                                        {
                                            p.Id = long.Parse(pageItemValue.N);
                                            break;
                                        }
                                        case ScoreDatabasePropertyNames.PagesItemId:
                                        {
                                            p.ItemId = pageItemValue.S;
                                            break;
                                        }
                                        case ScoreDatabasePropertyNames.PagesPage:
                                        {
                                            p.Page = pageItemValue.S;
                                            break;
                                        }
                                    }
                                }

                                pages.Add(p);
                            }
                        }
                        result.Page = pages;
                        break;
                    }
                    case ScoreDatabasePropertyNames.Annotations:
                    {
                        var annotations = new List<DatabaseScoreDataAnnotationV1>();
                        if (0 < v.L.Count)
                        {
                            foreach (var annotationValue in v.L)
                            {
                                if(annotationValue.M.Count == 0)
                                    continue;

                                var annotation = new DatabaseScoreDataAnnotationV1();
                                foreach (var (annotationItemKey,annotationItemValue) in annotationValue.M)
                                {
                                    switch (annotationItemKey)
                                    {
                                        case ScoreDatabasePropertyNames.AnnotationsId:
                                        {
                                            annotation.Id = long.Parse(annotationItemValue.N);
                                            break;
                                        }
                                        case ScoreDatabasePropertyNames.AnnotationsContent:
                                        {
                                            annotation.Content = annotationItemValue.S;
                                            break;
                                        }
                                    }
                                }

                                annotations.Add(annotation);
                            }
                        }
                        result.Annotations = annotations;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// データベースデータのクラスから DynamoDB のデータベースのデータに変換する
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static AttributeValue ConvertToDatabaseDataV1(DatabaseScoreDataV1 data)
        {
            var databaseData = new Dictionary<string, AttributeValue>();

            if (!string.IsNullOrWhiteSpace(data.Title))
            {
                databaseData[ScoreDatabasePropertyNames.Title] = new AttributeValue(data.Title);
            }
            if (!string.IsNullOrWhiteSpace(data.Description))
            {
                databaseData[ScoreDatabasePropertyNames.Description] = new AttributeValue(data.Description);
            }
            if (!string.IsNullOrWhiteSpace(data.Version))
            {
                databaseData[ScoreDatabasePropertyNames.DataVersion] = new AttributeValue(data.Version);
            }

            var pages = new List<AttributeValue>();
            if (0 < data.Page?.Count)
            {
                foreach (var value in data.Page)
                {
                    var page = new Dictionary<string, AttributeValue>
                    {
                        [ScoreDatabasePropertyNames.PagesId] = new AttributeValue() {N = value.Id.ToString()}
                    };

                    if (value.Page != null)
                    {
                        page[ScoreDatabasePropertyNames.PagesPage] = new AttributeValue(value.Page);
                    }
                    if (value.ItemId != null)
                    {
                        page[ScoreDatabasePropertyNames.PagesItemId] = new AttributeValue(value.ItemId);
                    }

                    if(page.Count == 0)
                        continue;
                    pages.Add(new AttributeValue() {M = page});
                }
            }

            databaseData[ScoreDatabasePropertyNames.Pages] = new AttributeValue() {L = pages, IsLSet = true};

            var annotations = new List<AttributeValue>();
            if (0 < data.Annotations?.Count)
            {
                foreach (var value in data.Annotations)
                {
                    var annotation = new Dictionary<string, AttributeValue>
                    {
                        [ScoreDatabasePropertyNames.AnnotationsId] = new AttributeValue() {N = value.Id.ToString()}
                    };

                    if (value.Content != null)
                    {
                        annotation[ScoreDatabasePropertyNames.AnnotationsContent] = new AttributeValue(value.Content);
                    }

                    if(annotation.Count == 0)
                        continue;
                    annotations.Add(new AttributeValue() {M = annotation});
                }
            }

            databaseData[ScoreDatabasePropertyNames.Annotations] =
                new AttributeValue() {L = annotations, IsLSet = true};

            return new AttributeValue() {M = databaseData};
        }

        /// <summary>
        /// <see cref="DateTimeOffset"/> から Unix millisecond の16進数表記に変換する
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static string ConvertToUnixTimeMilli(DateTimeOffset datetime) =>
            datetime.ToUnixTimeMilliseconds().ToString("X");

        /// <summary>
        /// Unix millisecond の16進数表記から <see cref="DateTimeOffset"/> に変換する
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static DateTimeOffset ConvertFromUnixTimeMilli(string datetime) =>
            DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(datetime, NumberStyles.HexNumber));

        /// <summary>
        /// 最小単位が Unix millisecond の現時間を取得する
        /// </summary>
        /// <returns></returns>
        public static DateTimeOffset UnixTimeMillisecondsNow() =>
            DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.Now.ToUnixTimeMilliseconds());

        /// <summary>
        /// アクセスを文字に変換する
        /// </summary>
        /// <param name="access"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string ConvertFromScoreAccess(ScoreAccesses access) => access switch
        {
            ScoreAccesses.Private => ScoreDatabaseConstant.ScoreAccessPrivate,
            ScoreAccesses.Public => ScoreDatabaseConstant.ScoreAccessPublic,
            _ => throw new InvalidOperationException()
        };
    }
}
