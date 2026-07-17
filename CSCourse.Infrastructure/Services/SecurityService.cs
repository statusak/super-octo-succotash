using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CSCourse.Application.Interfaces;
using CSCourse.Domain.Models;
using CSCourse.Infrastructure.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CSCourse.Infrastructure.Services;

public class SecurityService : ISecurityService
{
    private readonly JwtSettings _jwtSettings;

    public SecurityService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    /// <summary>
    /// Хеширует пароль с помощью SHA‑256 (без соли). 
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        byte[] inputBytes = Encoding.UTF8.GetBytes(password);
    
        // Compute the hash algorithm bytes
        byte[] hashBytes = SHA256.HashData(inputBytes);
        
        // Convert byte array to a clean uppercase Hexadecimal string
        return Convert.ToHexString(hashBytes).ToLower(); // Remove .ToLower()
    }

    /// <summary>
    /// Проверяет, соответствует ли пароль хешу (SHA‑256).
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        var computedHash = HashPassword(password);
        return ConstantTimeComparison(computedHash, hash);
    }

    /// <summary>
    /// Создаёт подписанный JWT‑токен на основе данных пользователя.
    /// </summary>
    public string CreateJwtToken(AccountJWTInfoDto accountJwtInfoDto)
    {
        if (accountJwtInfoDto == null)
            throw new ArgumentNullException(nameof(accountJwtInfoDto));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", accountJwtInfoDto.Id.ToString()),
            new Claim("login", accountJwtInfoDto.Login),
            new Claim("role", accountJwtInfoDto.Role.ToString()),

            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    // Сравнение хешей в константное время, чтобы избежать timing‑атак
    private bool ConstantTimeComparison(string a, string b)
    {
        if (a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}
