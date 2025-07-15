using System.Text.Json;
using System.Text.Json.Serialization;

namespace CHAP2.Domain.ValueObjects;

public class ChorusMetadata
{
    public string? Composer { get; set; }
    public string? Arranger { get; set; }
    public string? Copyright { get; set; }
    public string? Language { get; set; }
    public string? Genre { get; set; }
    public int? Tempo { get; set; }
    public string? Difficulty { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> CustomProperties { get; set; } = new();

    public ChorusMetadata()
    {
    }

    public ChorusMetadata(string? composer, string? arranger, string? copyright, string? language, string? genre, int? tempo, string? difficulty)
    {
        Composer = composer;
        Arranger = arranger;
        Copyright = copyright;
        Language = language;
        Genre = genre;
        Tempo = tempo;
        Difficulty = difficulty;
    }

    public void AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
        {
            Tags.Add(tag);
        }
    }

    public void RemoveTag(string tag)
    {
        Tags.Remove(tag);
    }

    public void SetCustomProperty(string key, string value)
    {
        CustomProperties[key] = value;
    }

    public string? GetCustomProperty(string key)
    {
        return CustomProperties.TryGetValue(key, out var value) ? value : null;
    }
}

// JSON converter for backward compatibility
public class ChorusMetadataJsonConverter : JsonConverter<ChorusMetadata>
{
    public override ChorusMetadata Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var metadata = new ChorusMetadata();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return metadata;
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
                    metadata.Composer = reader.GetString();
                    break;
                case "arranger":
                    metadata.Arranger = reader.GetString();
                    break;
                case "copyright":
                    metadata.Copyright = reader.GetString();
                    break;
                case "language":
                    metadata.Language = reader.GetString();
                    break;
                case "genre":
                    metadata.Genre = reader.GetString();
                    break;
                case "tempo":
                    if (reader.TokenType == JsonTokenType.Number)
                        metadata.Tempo = reader.GetInt32();
                    break;
                case "difficulty":
                    metadata.Difficulty = reader.GetString();
                    break;
                case "tags":
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                metadata.Tags.Add(reader.GetString() ?? string.Empty);
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
                                    metadata.CustomProperties[key ?? string.Empty] = reader.GetString() ?? string.Empty;
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