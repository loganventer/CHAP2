using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class AssignRoleRequestDto
{
    [Required]
    public string Role { get; set; } = string.Empty;
}
