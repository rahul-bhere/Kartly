using Kartly.Domain.Entities;

namespace Kartly.Application.Interfaces;

// -----------------------------------------------------------------------
// Isolates all JWT creation/validation logic behind one interface so the
// concrete implementation (Infrastructure/Security/JwtTokenService.cs)
// can be swapped or reconfigured (e.g. different signing algorithm)
// without touching AuthService.
// -----------------------------------------------------------------------
public interface IJwtTokenService
{
    // Short-lived token carrying UserId + Role claims, sent on every
    // authenticated request via the Authorization header.
    (string token, DateTime expiresAt) GenerateAccessToken(User user);

    // Long-lived, random opaque string stored (hashed) in the database,
    // used ONLY to obtain a new access token via POST /api/auth/refresh.
    string GenerateRefreshTokenValue();
}
