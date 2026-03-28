namespace CHAP2.Shared.DTOs;

public class ApiSearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public string SearchMode { get; set; } = string.Empty;
    public string SearchIn { get; set; } = string.Empty;
    public int Count { get; set; }
    public int MaxResults { get; set; }
    public List<ApiChorusDto> Results { get; set; } = new();
}
