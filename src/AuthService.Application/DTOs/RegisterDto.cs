using System.ComponentModel.DataAnnotations;

namespace AuthService.Application.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(25)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [MaxLength(25)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "CLIENT";

    public string? Description { get; set; }
    public string? Address { get; set; }
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}