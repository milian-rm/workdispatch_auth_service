using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Exceptions;
using AuthService.Application.Validators;
using AuthService.Domain.Constants;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AuthService.Application.DTOs.Email;
using AuthService.Application.Extensions;

namespace AuthService.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService,
    ICloudinaryService cloudinaryService,
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly ICloudinaryService _cloudinaryService = cloudinaryService;

    public async Task<RegisterResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        if (await userRepository.ExistsByEmailAsync(registerDto.Email))
        {
            logger.LogRegistrationWithExistingEmail();
            throw new BusinessException(ErrorCodes.EMAIL_ALREADY_EXISTS, "Email already exists");
        }

        string defaultProfilePhoto = _cloudinaryService.GetDefaultAvatarUrl();

        var emailVerificationToken = TokenGenerator.GenerateEmailVerificationToken();
        var userId = UuidGenerator.GenerateUserId();
        var userProfileId = UuidGenerator.GenerateUserId();
        var userEmailId = UuidGenerator.GenerateUserId();
        var userRoleId = UuidGenerator.GenerateUserId();

        string roleName = !string.IsNullOrEmpty(registerDto.Role) 
            ? registerDto.Role.ToUpper() 
            : RoleConstants.USER_ROLE;

        var defaultRole = await roleRepository.GetByNameAsync(roleName) 
                        ?? await roleRepository.GetByNameAsync(RoleConstants.USER_ROLE);

        if (defaultRole == null)
        {
            throw new InvalidOperationException("No se encontró un rol válido en el sistema.");
        }

        var user = new User
        {
            Id = userId,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email.ToLowerInvariant(),
            Password = passwordHashService.HashPassword(registerDto.Password),
            Status = false,
            UserProfile = new UserProfile
            {
                Id = userProfileId,
                UserId = userId,
                ProfilePhoto = defaultProfilePhoto, // Corregido: antes decía ProfilePictureUrl
                Phone = registerDto.Phone ?? string.Empty
            },
            UserEmail = new UserEmail
            {
                Id = userEmailId,
                UserId = userId,
                EmailVerified = false,
                EmailVerificationToken = emailVerificationToken,
                EmailVerificationTokenExpiration = DateTime.UtcNow.AddHours(24)
            },
            UserRoles =
            [
                new Domain.Entities.UserRole
                {
                    Id = userRoleId,
                    UserId = userId,
                    RoleId = defaultRole.Id
                }
            ]
        };

        var createdUser = await userRepository.CreateAsync(user);
        logger.LogUserRegistered(createdUser.Email);

        _ = Task.Run(async () =>
        {
            try
            {
                await emailService.SendEmailVerificationAsync(createdUser.Email, createdUser.FirstName, emailVerificationToken);
                logger.LogInformation("Verification email sent");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send verification email");
            }
        });

        return new RegisterResponseDto
        {
            Success = true,
            User = MapToUserResponseDto(createdUser),
            Message = "Usuario registrado exitosamente. Por favor, verifica tu email para activar la cuenta.",
            EmailVerificationRequired = true
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        User? user = await userRepository.GetByEmailAsync(loginDto.Email.ToLowerInvariant());

        if (user == null)
        {
            logger.LogFailedLoginAttempt();
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!user.Status)
        {
            logger.LogFailedLoginAttempt();
            throw new UnauthorizedAccessException("User account is disabled");
        }

        if (!passwordHashService.VerifyPassword(loginDto.Password, user.Password))
        {
            logger.LogFailedLoginAttempt();
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        logger.LogUserLoggedIn();

        var token = jwtTokenService.GenerateToken(user);
        var expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryInMinutes"] ?? "30");

        return new AuthResponseDto
        {
            Success = true,
            Message = "Login exitoso",
            Token = token,
            UserDetails = MapToUserDetailsDto(user),
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
        };
    }

    public async Task<EmailResponseDto> VerifyEmailAsync(VerifyEmailDto verifyEmailDto)
    {
        var user = await userRepository.GetByEmailVerificationTokenAsync(verifyEmailDto.Token);
        if (user == null || user.UserEmail == null)
        {
            return new EmailResponseDto { Success = false, Message = "Invalid or expired verification token" };
        }

        user.UserEmail.EmailVerified = true;
        user.Status = true;
        user.UserEmail.EmailVerificationToken = null;
        user.UserEmail.EmailVerificationTokenExpiration = null;

        await userRepository.UpdateAsync(user);

        try
        {
            await emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        return new EmailResponseDto { Success = true, Message = "Email verificado exitosamente" };
    }

    public async Task<EmailResponseDto> ResendVerificationEmailAsync(ResendVerificationDto resendDto)
    {
        var user = await userRepository.GetByEmailAsync(resendDto.Email);
        if (user == null || user.UserEmail == null)
        {
            return new EmailResponseDto { Success = false, Message = "Usuario no encontrado" };
        }

        var newToken = TokenGenerator.GenerateEmailVerificationToken();
        user.UserEmail.EmailVerificationToken = newToken;
        user.UserEmail.EmailVerificationTokenExpiration = DateTime.UtcNow.AddHours(24);

        await userRepository.UpdateAsync(user);

        try
        {
            await emailService.SendEmailVerificationAsync(user.Email, user.FirstName, newToken);
            return new EmailResponseDto { Success = true, Message = "Email de verificación enviado exitosamente" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resend verification email");
            return new EmailResponseDto { Success = false, Message = "Error al enviar el email" };
        }
    }

    public async Task<EmailResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        var user = await userRepository.GetByEmailAsync(forgotPasswordDto.Email);
        if (user == null)
        {
            return new EmailResponseDto { Success = true, Message = "Si el email existe, se ha enviado un enlace de recuperación" };
        }

        var resetToken = TokenGenerator.GeneratePasswordResetToken();
        if (user.UserPasswordReset == null)
        {
            user.UserPasswordReset = new UserPasswordReset { UserId = user.Id, PasswordResetToken = resetToken, PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1) };
        }
        else
        {
            user.UserPasswordReset.PasswordResetToken = resetToken;
            user.UserPasswordReset.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(1);
        }

        await userRepository.UpdateAsync(user);

        try
        {
            await emailService.SendPasswordResetAsync(user.Email, user.FirstName, resetToken);
        }
        catch (Exception ex) { logger.LogError(ex, "Error sending password reset"); }

        return new EmailResponseDto { Success = true, Message = "Si el email existe, se ha enviado un enlace de recuperación" };
    }

    public async Task<EmailResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        var user = await userRepository.GetByPasswordResetTokenAsync(resetPasswordDto.Token);
        if (user == null || user.UserPasswordReset == null)
        {
            return new EmailResponseDto { Success = false, Message = "Token inválido" };
        }

        user.Password = passwordHashService.HashPassword(resetPasswordDto.NewPassword);
        user.UserPasswordReset.PasswordResetToken = string.Empty;
        user.UserPasswordReset.PasswordResetTokenExpiration = DateTime.UtcNow.AddDays(-1);

        await userRepository.UpdateAsync(user);
        return new EmailResponseDto { Success = true, Message = "Contraseña actualizada" };
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(string userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        return user == null ? null : MapToUserResponseDto(user);
    }

    private UserResponseDto MapToUserResponseDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ProfilePhoto = _cloudinaryService.GetFullImageUrl(user.UserProfile?.ProfilePhoto ?? string.Empty), // Corregido el nombre de propiedad
            Phone = user.UserProfile?.Phone ?? string.Empty,
            Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? RoleConstants.USER_ROLE,
            Status = user.Status,
            IsEmailVerified = user.UserEmail?.EmailVerified ?? false,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt // Ahora consistente con el DTO
        };
    }

    private UserDetailsDto MapToUserDetailsDto(User user)
    {
        return new UserDetailsDto
        {
            Id = user.Id,
            Email = user.Email,
            ProfilePhoto = _cloudinaryService.GetFullImageUrl(user.UserProfile?.ProfilePhoto ?? string.Empty), // Corregido el nombre de propiedad
            Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? RoleConstants.USER_ROLE
        };
    }
}