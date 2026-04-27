using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class UpdateUserPreferencesRequestDto
{
    [Required]
    public string Theme { get; set; } = string.Empty;

    [Required]
    public string DefaultSearchScope { get; set; } = string.Empty;

    [Required]
    public string Language { get; set; } = string.Empty;
}
