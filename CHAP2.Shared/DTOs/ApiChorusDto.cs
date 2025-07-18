namespace CHAP2.Shared.DTOs;

/// <summary>
/// API-specific DTOs for internal API communication
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

public class ApiSearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public string SearchMode { get; set; } = string.Empty;
    public string SearchIn { get; set; } = string.Empty;
    public int Count { get; set; }
    public int MaxResults { get; set; }
    public List<ApiChorusDto> Results { get; set; } = new();
}

public class ApiSlideConversionResponseDto
{
    public string Message { get; set; } = string.Empty;
    public ApiChorusDto Chorus { get; set; } = new();
    public string OriginalFilename { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
} 