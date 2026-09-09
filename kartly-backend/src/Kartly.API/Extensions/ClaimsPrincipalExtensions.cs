using System.Security.Claims;

namespace Kartly.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated request is missing a user id claim.");
        return Guid.Parse(idClaim);
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole("Admin");
}
