using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class ResetPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string ResetToken { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
