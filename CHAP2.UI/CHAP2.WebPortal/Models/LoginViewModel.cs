using System.ComponentModel.DataAnnotations;

namespace CHAP2.WebPortal.Models;

public sealed class LoginViewModel
{
    [Required]
    public string Username { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
