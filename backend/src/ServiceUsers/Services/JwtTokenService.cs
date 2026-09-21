using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ServiceUsers.Data.Entities;
using StudentPass.Contracts;

namespace ServiceUsers.Services;

public sealed class JwtTokenService
{
    private readonly string _secret;
    private readonly string _algorithm;
    private readonly int _expiresMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["JWT_SECRET_KEY"] ?? configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT_SECRET_KEY is not configured");
        _algorithm = configuration["JWT_ALGORITHM"] ?? configuration["Jwt:Algorithm"] ?? "HS256";
        _expiresMinutes = int.TryParse(
            configuration["ACC_TOKEN_EXP_MIN"] ?? configuration["Jwt:ExpiresMinutes"],
            out var minutes)
            ? minutes
            : 60;
    }

    public int ExpiresMinutes => _expiresMinutes;

    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, MapAlgorithm(_algorithm));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim("user_id", user.Id.ToString()),
            new Claim("role", user.Role.ToApi()),
            new Claim("full_name", user.FullName)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiresMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string MapAlgorithm(string algorithm) => algorithm.ToUpperInvariant() switch
    {
        "HS256" => SecurityAlgorithms.HmacSha256,
        "HS384" => SecurityAlgorithms.HmacSha384,
        "HS512" => SecurityAlgorithms.HmacSha512,
        _ => SecurityAlgorithms.HmacSha256
    };
}
