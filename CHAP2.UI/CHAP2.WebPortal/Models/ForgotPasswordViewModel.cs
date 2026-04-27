using System.ComponentModel.DataAnnotations;

namespace CHAP2.WebPortal.Models;

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
