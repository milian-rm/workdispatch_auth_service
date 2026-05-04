namespace AuthService.Application.DTOs;

public class AuthResponseDto {
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Token { get; set; }
    public UserDetailsDto UserDetails { get; set; }
    public DateTime ExpiresAt { get; set; }
}