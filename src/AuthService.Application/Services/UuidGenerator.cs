using System.Security.Cryptography;
using System.Text;

namespace AuthService.Application.Services;

public static class UuidGenerator
{
    private static string Alphabet = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    public static string GenerateShortUUID()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[12];
        rng.GetBytes(bytes);

        var result = new StringBuilder(12);

        for (int i = 0; i < 12; i++)
        {
            result.Append(Alphabet[bytes[i] % Alphabet.Length]);
        }

        return result.ToString();
    }

    public static string GenerateUserId()
    {
        return $"usr_{GenerateShortUUID()}";
    }

    public static string GenerateRoleId()
    {
        return $"rol_{GenerateShortUUID()}";
    }

    public static bool IsValidUserId(string id)
    {
        if(string.IsNullOrEmpty(id))
        {
            return false;
        }

        if(id.Length != 12 || !id.StartsWith("usr_"))
        {
            return false;
        }

        //Toma el string desde el 5to caracter
        var idPart = id[4..];
        return idPart.All(c => Alphabet.Contains(c));
    }
}