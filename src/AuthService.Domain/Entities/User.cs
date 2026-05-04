using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Entities;

public class User
{
    [Key]
    [MaxLength(16)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(25)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(25)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(255)] // Siempre para el Hash de Argon2
    public string Password { get; set; } = string.Empty;

    public bool Status { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relaciones de navegación 
    public UserProfile UserProfile { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public UserEmail UserEmail { get; set; } = null!;
    public UserPasswordReset UserPasswordReset { get; set; } = null!;
}