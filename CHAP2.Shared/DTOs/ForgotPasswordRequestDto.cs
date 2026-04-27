using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
