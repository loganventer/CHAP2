using System.ComponentModel.DataAnnotations;
using CHAP2.Domain.Enums;

namespace CHAP2.Chorus.Api.Requests;

public class UpdateChorusRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string ChorusText { get; set; } = string.Empty;

    public MusicalKey Key { get; set; }
    public ChorusType Type { get; set; }
    public TimeSignature TimeSignature { get; set; }
}
