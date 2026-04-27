using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class RenameSetlistRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}
