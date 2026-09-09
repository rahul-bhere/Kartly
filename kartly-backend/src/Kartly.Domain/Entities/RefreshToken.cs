using Kartly.Domain.Common;

namespace Kartly.Domain.Entities;

// -----------------------------------------------------------------------
// Refresh tokens let a client get a new short-lived access token without
// forcing the user to log in again, while still allowing the server to
// revoke a session (e.g. on logout or if a token is compromised).
// -----------------------------------------------------------------------
public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => RevokedAt == null && !IsExpired;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
