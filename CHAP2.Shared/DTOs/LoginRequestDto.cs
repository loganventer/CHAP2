using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class LoginRequestDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
