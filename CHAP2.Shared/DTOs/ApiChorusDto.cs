namespace CHAP2.Shared.DTOs;

/// <summary>
/// API-specific DTO for internal API communication
/// </summary>
public class ApiChorusDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ChorusText { get; set; } = string.Empty;
    public int Key { get; set; }
    public int Type { get; set; }
    public int TimeSignature { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public object? Metadata { get; set; }
    public List<object> DomainEvents { get; set; } = new();
}
