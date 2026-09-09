using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kartly.Infrastructure.Security;

// -----------------------------------------------------------------------
// Reads its signing key and lifetime settings from configuration
// (appsettings / user-secrets / environment variables) — see the "Jwt"
// section documented in the backend README. NEVER hardcode the signing
// key here.
// -----------------------------------------------------------------------
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    // HMAC-SHA256 requires a key of at least 128 bits (16 bytes) per the
    // algorithm's spec, and Microsoft.IdentityModel.Tokens enforces this
    // by throwing a low-level, unhelpfully-worded exception
    // ("IDX10653: ... requires a key size of at least '128' bits...")
    // if it's violated. Checking it here ourselves turns that into a
    // clear, actionable message instead of a mysterious 500.
    private const int MinimumKeyBytes = 16;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string token, DateTime expiresAt) GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? string.Empty);

        if (keyBytes.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:Key is missing or too short ({keyBytes.Length} bytes; needs at least {MinimumKeyBytes}). " +
                "Set a real secret: `dotnet user-secrets set \"Jwt:Key\" \"<32+ char random string>\"` " +
                "from inside src/Kartly.API — see the backend README's 'Set the JWT signing secret' section. " +
                "Generate one with `openssl rand -base64 48` (or the PowerShell equivalent in the README).");
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(jwtSettings["AccessTokenExpiryMinutes"] ?? "30");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        // These two claims are exactly what [Authorize(Roles = "Admin")]
        // and User.Identity checks in the controllers rely on.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshTokenValue()
    {
        // A cryptographically random opaque string — it carries no
        // claims itself, it's just a lookup key stored in the
        // RefreshTokens table (see RefreshTokenRepository).
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }
}
