using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace ScoreHistoryApi.Models.Objects
{
    /// <summary>
    /// 楽譜オブジェクトの情報
    /// </summary>
    public class ScoreObjectInfo
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [BindProperty(Name = "originalName")]
        public string OriginalName { get; set; }
    }
}
