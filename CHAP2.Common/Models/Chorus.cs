using System.ComponentModel.DataAnnotations;
using CHAP2.Common.Enum;

namespace CHAP2.Common.Models;

public class Chorus
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public MusicalKey Key { get; set; }
    [Required]
    public TimeSignature TimeSignature { get; set; }
    [Required]
    public string ChorusText { get; set; } = string.Empty;
    [Required]
    public ChorusType Type { get; set; }
} 