using System.Security.Cryptography;

namespace ExpenseManager.Services;

public static class PasswordService
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }
    public static bool Verify(string password, string stored)
    {
        try { var p = stored.Split('.'); var key = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(p[0]), 100_000, HashAlgorithmName.SHA256, 32); return CryptographicOperations.FixedTimeEquals(key, Convert.FromBase64String(p[1])); }
        catch { return false; }
    }
}
