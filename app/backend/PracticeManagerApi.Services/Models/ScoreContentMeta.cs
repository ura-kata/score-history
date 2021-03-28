using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PracticeManagerApi.Services.Models
{
    public class ScoreContentMeta
    {
        [JsonPropertyName(name: "name")]
        public string Name { get; set; }

        [JsonPropertyName(name: "title")]
        public string Title { get; set; }

        [JsonPropertyName(name: "description")]
        public string Description { get; set; }

        [JsonPropertyName(name: "version_file_keys")]
        public Dictionary<string,string> VersionFileKeys { get; set; } = new Dictionary<string, string>();

        public async Task<ScoreContentMeta> DeepCopyAsync()
        {
            using var mem = new MemoryStream();
            var options = new JsonSerializerOptions();
            await JsonSerializer.SerializeAsync(mem, this, options);

            mem.Position = 0;

            return await JsonSerializer.DeserializeAsync<ScoreContentMeta>(mem, options);
        }
    }
}
