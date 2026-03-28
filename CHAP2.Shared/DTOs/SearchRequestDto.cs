using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class SearchRequestDto
{
    [Required]
    public string Query { get; set; } = string.Empty;
    public string SearchMode { get; set; } = "Contains";
    public string SearchIn { get; set; } = "all";
}
