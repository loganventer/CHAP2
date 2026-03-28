namespace CHAP2.Domain.ValueObjects;

public class ChorusMetadata
{
    public string? Composer { get; init; }
    public string? Arranger { get; init; }
    public string? Copyright { get; init; }
    public string? Language { get; init; }
    public string? Genre { get; init; }
    public int? Tempo { get; init; }
    public string? Difficulty { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> CustomProperties { get; init; } = new Dictionary<string, string>();

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

    public ChorusMetadata WithTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || Tags.Contains(tag))
            return this;

        var newTags = new List<string>(Tags) { tag };
        return new ChorusMetadata
        {
            Composer = Composer,
            Arranger = Arranger,
            Copyright = Copyright,
            Language = Language,
            Genre = Genre,
            Tempo = Tempo,
            Difficulty = Difficulty,
            Tags = newTags,
            CustomProperties = CustomProperties
        };
    }

    public ChorusMetadata WithoutTag(string tag)
    {
        var newTags = new List<string>(Tags);
        newTags.Remove(tag);
        return new ChorusMetadata
        {
            Composer = Composer,
            Arranger = Arranger,
            Copyright = Copyright,
            Language = Language,
            Genre = Genre,
            Tempo = Tempo,
            Difficulty = Difficulty,
            Tags = newTags,
            CustomProperties = CustomProperties
        };
    }

    public ChorusMetadata WithCustomProperty(string key, string value)
    {
        var newProps = new Dictionary<string, string>(CustomProperties)
        {
            [key] = value
        };
        return new ChorusMetadata
        {
            Composer = Composer,
            Arranger = Arranger,
            Copyright = Copyright,
            Language = Language,
            Genre = Genre,
            Tempo = Tempo,
            Difficulty = Difficulty,
            Tags = Tags,
            CustomProperties = newProps
        };
    }

    public string? GetCustomProperty(string key)
    {
        return CustomProperties.TryGetValue(key, out var value) ? value : null;
    }
}
