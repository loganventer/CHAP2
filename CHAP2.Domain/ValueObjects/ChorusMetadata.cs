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