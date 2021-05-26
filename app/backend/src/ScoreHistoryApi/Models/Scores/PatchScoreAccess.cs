using System.Text.Json.Serialization;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.Models.Scores
{
    /// <summary>
    /// 楽譜のアクセスの設定
    /// </summary>
    public class PatchScoreAccess
    {
        /// <summary> アクセス </summary>
        [JsonPropertyName("access")]
        public ScoreAccesses Access { get; set; }
    }
}
