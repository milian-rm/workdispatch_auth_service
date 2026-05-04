namespace AuthService.Application.DTOs;

public class UserResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePhoto { get; set; } 
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool Status { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } 
}