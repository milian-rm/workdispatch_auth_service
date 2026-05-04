using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Domain.Entities;

public class UserProfile
{
    [Key]
    [MaxLength(16)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string UserId { get; set; } = string.Empty;

    public string? ProfilePhoto { get; set; } 
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public decimal RatingAverage { get; set; } = 1.0m;

    public bool VerificationStatus { get; set; } = false;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
