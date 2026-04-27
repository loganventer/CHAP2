using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class ReorderSetlistRequestDto
{
    [Required]
    public IReadOnlyList<Guid> ItemIdsInOrder { get; set; } = Array.Empty<Guid>();
}
