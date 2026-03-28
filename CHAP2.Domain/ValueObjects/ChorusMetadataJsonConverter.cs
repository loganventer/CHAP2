using System.Text.Json;
using System.Text.Json.Serialization;

namespace CHAP2.Domain.ValueObjects;

public class ChorusMetadataJsonConverter : JsonConverter<ChorusMetadata>
{
    public override ChorusMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        string? composer = null;
        string? arranger = null;
        string? copyright = null;
        string? language = null;
        string? genre = null;
        int? tempo = null;
        string? difficulty = null;
        var tags = new List<string>();
        var customProperties = new Dictionary<string, string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new ChorusMetadata(composer, arranger, copyright, language, genre, tempo, difficulty)
                {
                    Tags = tags,
                    CustomProperties = customProperties
                };
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "composer":
                    composer = reader.GetString();
                    break;
                case "arranger":
                    arranger = reader.GetString();
                    break;
                case "copyright":
                    copyright = reader.GetString();
                    break;
                case "language":
                    language = reader.GetString();
                    break;
                case "genre":
                    genre = reader.GetString();
                    break;
                case "tempo":
                    if (reader.TokenType == JsonTokenType.Number)
                        tempo = reader.GetInt32();
                    break;
                case "difficulty":
                    difficulty = reader.GetString();
                    break;
                case "tags":
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                tags.Add(reader.GetString() ?? string.Empty);
                            }
                        }
                    }
                    break;
                case "customproperties":
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName)
                            {
                                var key = reader.GetString();
                                reader.Read();
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    customProperties[key ?? string.Empty] = reader.GetString() ?? string.Empty;
                                }
                            }
                        }
                    }
                    break;
                default:
                    // Skip unknown properties for backward compatibility
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, ChorusMetadata value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(value.Composer))
            writer.WriteString("composer", value.Composer);

        if (!string.IsNullOrEmpty(value.Arranger))
            writer.WriteString("arranger", value.Arranger);

        if (!string.IsNullOrEmpty(value.Copyright))
            writer.WriteString("copyright", value.Copyright);

        if (!string.IsNullOrEmpty(value.Language))
            writer.WriteString("language", value.Language);

        if (!string.IsNullOrEmpty(value.Genre))
            writer.WriteString("genre", value.Genre);

        if (value.Tempo.HasValue)
            writer.WriteNumber("tempo", value.Tempo.Value);

        if (!string.IsNullOrEmpty(value.Difficulty))
            writer.WriteString("difficulty", value.Difficulty);

        if (value.Tags.Count > 0)
        {
            writer.WriteStartArray("tags");
            foreach (var tag in value.Tags)
            {
                writer.WriteStringValue(tag);
            }
            writer.WriteEndArray();
        }

        if (value.CustomProperties.Count > 0)
        {
            writer.WriteStartObject("customProperties");
            foreach (var kvp in value.CustomProperties)
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
