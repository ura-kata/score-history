using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Objects
{
    public class ScoreV2CommentObject : ScoreV2Object
    {
        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }
}
