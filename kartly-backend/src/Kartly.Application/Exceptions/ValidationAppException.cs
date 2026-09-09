namespace Kartly.Application.Exceptions;

// Thrown for business-rule validation failures (distinct from
// FluentValidation's request-shape validation, which is handled by the
// validation pipeline before a request even reaches a service).
// Turned into a 400 response by the exception middleware.
public class ValidationAppException : Exception
{
    public ValidationAppException(string message) : base(message) { }
}
