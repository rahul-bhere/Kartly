namespace Kartly.Application.Exceptions;

// e.g. a non-admin trying to reach an admin-only operation that slipped
// past the [Authorize(Roles = "Admin")] check for some reason, or a user
// trying to act on another user's resource. Turned into a 403 response.
public class ForbiddenAppException : Exception
{
    public ForbiddenAppException(string message) : base(message) { }
}
