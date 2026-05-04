using Microsoft.Extensions.Logging;
 
namespace AuthService.Application.Extensions;
 
public static class AuthServiceLoggerExtensions
{
    public static void LogRegistrationWithExistingEmail(this ILogger logger)
    {
        logger.LogWarning("Intento de registro fallido: El correo ya existe.");
    }
 
    public static void LogRegistrationWithExistingUsername(this ILogger logger)
    {
        logger.LogWarning("Intento de registro fallido: El usuario ya existe.");
    }
 
    public static void LogImageUploadError(this ILogger logger)
    {
        logger.LogError("Error al intentar subir la imagen de perfil.");
    }
 
    public static void LogUserRegistered(this ILogger logger, string username)
    {
        logger.LogInformation("Usuario registrado exitosamente: {Username}", username);
    }
 
    public static void LogFailedLoginAttempt(this ILogger logger)
    {
        logger.LogWarning("Intento de login fallido: Credenciales incorrectas o cuenta inactiva.");
    }
 
    public static void LogUserLoggedIn(this ILogger logger)
    {
        logger.LogInformation("Usuario logueado exitosamente.");
    }
}