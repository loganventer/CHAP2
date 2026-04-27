using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
