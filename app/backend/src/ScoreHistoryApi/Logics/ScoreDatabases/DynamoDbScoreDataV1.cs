using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.Model;

namespace ScoreHistoryApi.Logics.ScoreDatabases
{
    /// <summary>
    /// DynamoDB の楽譜アイテムに含まれるデータの構造
    /// </summary>
    public class DynamoDbScoreDataV1: DynamoDbScoreDataBase
    {
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.Title)]
        public string Title { get; set; }

        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash)]
        public string DescriptionHash { get; set; }

        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.DataVersion)]
        public string Version { get; set; } = ScoreDatabaseConstant.ScoreDataVersion1;

        private List<DynamoDbScorePageV1> _page;
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.Pages)]
        public List<DynamoDbScorePageV1> Page
        {
            get => _page ??= new List<DynamoDbScorePageV1>();
            set => _page = value;
        }

        private List<DynamoDbScoreAnnotationV1> _annotations;
        [JsonPropertyName(DynamoDbScorePropertyNames.DataPropertyNames.Annotations)]
        public List<DynamoDbScoreAnnotationV1> Annotations
        {
            get => _annotations ??= new List<DynamoDbScoreAnnotationV1>();
            set => _annotations = value;
        }

        public override string CalcDataHash()
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

            var json = JsonSerializer.SerializeToUtf8Bytes(this, option);
            return Convert.ToBase64String(MD5.Create().ComputeHash(json));
        }


        /// <summary>
        /// DynamoDB の <see cref="AttributeValue"/> からクラスにマッピングする
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryMapFromAttributeValue(AttributeValue value, out DynamoDbScoreDataV1 data)
        {
            data = null;
            if (!value.M.TryGetValue(DynamoDbScorePropertyNames.DataPropertyNames.DataVersion, out var versionValue))
                return false;

            if (versionValue.S != ScoreDatabaseConstant.ScoreDataVersion1)
            {
                return false;
            }

            data = new DynamoDbScoreDataV1();

            foreach (var (key, v) in value.M)
            {
                switch (key)
                {
                    case DynamoDbScorePropertyNames.DataPropertyNames.Title:
                    {
                        data.Title = v.S;
                        break;
                    }
                    case DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash:
                    {
                        data.DescriptionHash = v.S;
                        break;
                    }
                    case DynamoDbScorePropertyNames.DataPropertyNames.DataVersion:
                    {
                        data.Version = v.S;
                        break;
                    }
                    case DynamoDbScorePropertyNames.DataPropertyNames.Pages:
                    {
                        var pages = new List<DynamoDbScorePageV1>();
                        if (0 < v.L.Count)
                        {
                            foreach (var pageValue in v.L)
                            {
                                if(pageValue.M.Count == 0)
                                    continue;

                                var p = new DynamoDbScorePageV1();
                                foreach (var (pageItemKey,pageItemValue) in pageValue.M)
                                {
                                    switch (pageItemKey)
                                    {
                                        case DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Id:
                                        {
                                            p.Id = long.Parse(pageItemValue.N, CultureInfo.InvariantCulture);
                                            break;
                                        }
                                        case DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.ItemId:
                                        {
                                            p.ItemId = pageItemValue.S;
                                            break;
                                        }
                                        case DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Page:
                                        {
                                            p.Page = pageItemValue.S;
                                            break;
                                        }
                                    }
                                }

                                pages.Add(p);
                            }
                        }
                        data.Page = pages;
                        break;
                    }
                    case DynamoDbScorePropertyNames.DataPropertyNames.Annotations:
                    {
                        var annotations = new List<DynamoDbScoreAnnotationV1>();
                        if (0 < v.L.Count)
                        {
                            foreach (var annotationValue in v.L)
                            {
                                if(annotationValue.M.Count == 0)
                                    continue;

                                var annotation = new DynamoDbScoreAnnotationV1();
                                foreach (var (annotationItemKey,annotationItemValue) in annotationValue.M)
                                {
                                    switch (annotationItemKey)
                                    {
                                        case DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.Id:
                                        {
                                            annotation.Id = long.Parse(annotationItemValue.N, CultureInfo.InvariantCulture);
                                            break;
                                        }
                                        case DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.ContentHash:
                                        {
                                            annotation.ContentHash = annotationItemValue.S;
                                            break;
                                        }
                                    }
                                }

                                annotations.Add(annotation);
                            }
                        }
                        data.Annotations = annotations;
                        break;
                    }
                }
            }

            return true;
        }

        public override AttributeValue ConvertToAttributeValue()
        {
            var databaseData = new Dictionary<string, AttributeValue>();

            if (!string.IsNullOrWhiteSpace(Title))
            {
                databaseData[DynamoDbScorePropertyNames.DataPropertyNames.Title] = new AttributeValue(Title);
            }
            if (!string.IsNullOrWhiteSpace(DescriptionHash))
            {
                databaseData[DynamoDbScorePropertyNames.DataPropertyNames.DescriptionHash] = new AttributeValue(DescriptionHash);
            }
            if (!string.IsNullOrWhiteSpace(Version))
            {
                databaseData[DynamoDbScorePropertyNames.DataPropertyNames.DataVersion] = new AttributeValue(Version);
            }

            var pages = new List<AttributeValue>();
            if (0 < Page?.Count)
            {
                foreach (var value in Page)
                {
                    var page = new Dictionary<string, AttributeValue>
                    {
                        [DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Id] = new AttributeValue() {N = value.Id.ToString()}
                    };

                    if (value.Page != null)
                    {
                        page[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.Page] = new AttributeValue(value.Page);
                    }
                    if (value.ItemId != null)
                    {
                        page[DynamoDbScorePropertyNames.DataPropertyNames.PagesPropertyNames.ItemId] = new AttributeValue(value.ItemId);
                    }

                    if(page.Count == 0)
                        continue;
                    pages.Add(new AttributeValue() {M = page});
                }
            }

            databaseData[DynamoDbScorePropertyNames.DataPropertyNames.PageCount] =
                new AttributeValue() {N = pages.Count.ToString()};
            databaseData[DynamoDbScorePropertyNames.DataPropertyNames.Pages] = new AttributeValue() {L = pages, IsLSet = true};

            var annotations = new List<AttributeValue>();
            if (0 < Annotations?.Count)
            {
                foreach (var value in Annotations)
                {
                    var annotation = new Dictionary<string, AttributeValue>
                    {
                        [DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.Id] = new AttributeValue() {N = value.Id.ToString()}
                    };

                    if (value.ContentHash != null)
                    {
                        annotation[DynamoDbScorePropertyNames.DataPropertyNames.AnnotationsPropertyNames.ContentHash] = new AttributeValue(value.ContentHash);
                    }

                    if(annotation.Count == 0)
                        continue;
                    annotations.Add(new AttributeValue() {M = annotation});
                }
            }

            databaseData[DynamoDbScorePropertyNames.DataPropertyNames.AnnotationCount] =
                new AttributeValue() {N = annotations.Count.ToString()};
            databaseData[DynamoDbScorePropertyNames.DataPropertyNames.Annotations] =
                new AttributeValue() {L = annotations, IsLSet = true};

            return new AttributeValue() {M = databaseData};
        }
    }
}
