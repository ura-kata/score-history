using System.Text.Json.Serialization;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreVersionPageMeta
    {
        [JsonPropertyName(name: "no")]
        public string No { get; set; }

        [JsonPropertyName(name: "image_file_key")]
        public string ImageFileKey { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "comment_prefix")]
        public string CommentPrefix { get; set; }

        [JsonPropertyName(name: "overlay_svg_key")]
        public string OverlaySvgKey { get; set; }
    }
}
