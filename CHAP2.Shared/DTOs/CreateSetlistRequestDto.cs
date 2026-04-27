using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class CreateSetlistRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
