using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class ConfirmEmailRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ConfirmationToken { get; set; } = string.Empty;
}
