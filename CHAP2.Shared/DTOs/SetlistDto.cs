namespace CHAP2.Shared.DTOs;

public class SetlistDto
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<SetlistItemDto> Items { get; set; } = Array.Empty<SetlistItemDto>();
}
