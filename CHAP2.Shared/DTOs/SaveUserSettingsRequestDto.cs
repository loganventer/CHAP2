using System.ComponentModel.DataAnnotations;

namespace CHAP2.Shared.DTOs;

public class SaveUserSettingsRequestDto
{
    /// <summary>
    /// Opaque JSON blob owned by the JS settings layer. Server stores it
    /// as-is and scopes by the calling user. 64KB cap is generous for any
    /// realistic preferences payload.
    /// </summary>
    [Required]
    [StringLength(65536)]
    public string Json { get; set; } = string.Empty;
}
