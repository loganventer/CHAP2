namespace CHAP2.Shared.DTOs;

public class SetlistItemPayloadDto
{
    public string Kind { get; set; } = "chorus";

    // chorus
    public Guid? ChorusId { get; set; }

    // verse
    public string? BookId { get; set; }
    public string? BookName { get; set; }
    public int? Chapter { get; set; }
    public int? Verse { get; set; }
    public string? Text { get; set; }
    public string? Ref { get; set; }
}
