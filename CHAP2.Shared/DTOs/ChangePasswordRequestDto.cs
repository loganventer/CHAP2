using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
