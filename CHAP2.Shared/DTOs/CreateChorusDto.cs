using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class CreateChorusDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string ChorusText { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TimeSignature { get; set; } = string.Empty;
    public ChorusMetadataDto? Metadata { get; set; }
}
