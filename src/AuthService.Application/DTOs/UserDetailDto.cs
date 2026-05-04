namespace AuthService.Application.DTOs;

public class UserDetailsDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Usamos Email como identificador principal
    public string? ProfilePhoto { get; set; } 
    public string Role { get; set; } = string.Empty;
}