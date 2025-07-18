using System.ComponentModel.DataAnnotations;
using CHAP2.Domain.Enums;

namespace CHAP2.Shared.ViewModels;

public class ChorusEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Chorus text is required")]
    public string ChorusText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Musical key is required")]
    public MusicalKey Key { get; set; }

    [Required(ErrorMessage = "Chorus type is required")]
    public ChorusType Type { get; set; }

    [Required(ErrorMessage = "Time signature is required")]
    public TimeSignature TimeSignature { get; set; }
} 