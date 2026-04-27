using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class SaveSetlistRequestDto
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<SetlistItemPayloadDto> Items { get; set; } = Array.Empty<SetlistItemPayloadDto>();
}
