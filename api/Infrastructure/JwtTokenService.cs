using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Api.Infrastructure;

public class JwtTokenService
{
    private readonly string _jwtKey;

    public JwtTokenService(IConfiguration configuration)
    {
        _jwtKey = configuration["JWT_KEY"] ?? Environment.GetEnvironmentVariable("JWT_KEY") ?? "dev_secret_key_change_me";
    }

    public string IssueToken(string username, bool isAdmin)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, isAdmin ? "admin" : "user")
        };
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

