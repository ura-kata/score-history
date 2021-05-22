using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScoreHistoryApi.Logics.ScoreDatabases;

namespace ScoreHistoryApi.JsonConverters
{
    public class ScoreAccessesJsonConverter: JsonConverter<ScoreAccesses>
    {
        public override ScoreAccesses Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.GetString().ToLower(CultureInfo.InvariantCulture);

            return text switch
            {
                ScoreDatabaseConstant.ScoreAccessPublic => ScoreAccesses.Public,
                ScoreDatabaseConstant.ScoreAccessPrivate => ScoreAccesses.Private,
                _ => throw new JsonException()
            };
        }

        public override void Write(Utf8JsonWriter writer, ScoreAccesses value, JsonSerializerOptions options)
        {
            var text = value switch
            {
                ScoreAccesses.Public => ScoreDatabaseConstant.ScoreAccessPublic,
                ScoreAccesses.Private => ScoreDatabaseConstant.ScoreAccessPrivate,
                _ => throw new JsonException()
            };

            writer.WriteStringValue(text);
        }
    }
}
