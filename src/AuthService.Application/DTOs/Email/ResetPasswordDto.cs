using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs.Email;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "El email electrónico es obligatorio.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El token es obligatorio.")]
    public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}