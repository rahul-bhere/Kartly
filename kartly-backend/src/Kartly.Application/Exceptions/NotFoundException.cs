namespace Kartly.Application.Exceptions;

// Thrown when a requested entity doesn't exist. Caught by the API's
// global exception middleware and turned into a 404 response.
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }
}
