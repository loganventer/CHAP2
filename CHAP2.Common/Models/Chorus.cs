using System.ComponentModel.DataAnnotations;
using CHAP2.Common.Enum;

namespace CHAP2.Common.Models;

public class Chorus
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required(ErrorMessage = "Chorus name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Chorus name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Musical key is required")]
    public MusicalKey Key { get; set; } = MusicalKey.NotSet;
    
    [Required(ErrorMessage = "Time signature is required")]
    public TimeSignature TimeSignature { get; set; } = TimeSignature.NotSet;
    
    [Required(ErrorMessage = "Chorus text is required")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Chorus text must be between 1 and 1000 characters")]
    public string ChorusText { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Chorus type is required")]
    public ChorusType Type { get; set; } = ChorusType.NotSet;
} 