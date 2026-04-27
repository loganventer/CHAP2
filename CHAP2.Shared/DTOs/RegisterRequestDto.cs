using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class RegisterRequestDto
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
