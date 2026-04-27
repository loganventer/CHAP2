using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class AppendChorusRequestDto
{
    [Required]
    public Guid ChorusId { get; set; }
}
