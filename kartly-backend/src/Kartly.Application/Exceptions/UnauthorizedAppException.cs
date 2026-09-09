namespace Kartly.Application.Exceptions;

// e.g. wrong username/password on login. Turned into a 401 response.
public class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}
