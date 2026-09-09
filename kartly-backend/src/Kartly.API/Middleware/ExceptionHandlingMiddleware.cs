using System.Net;
using System.Text.Json;
using Kartly.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Kartly.API.Middleware;

// -----------------------------------------------------------------------
// Catches exceptions thrown anywhere in the request pipeline and converts
// them into a consistent JSON error response with the right HTTP status
// code, instead of every controller needing its own try/catch blocks.
//
// WHAT THE CLIENT SEES vs WHAT GETS LOGGED:
// The response body ALWAYS contains a short, generic, user-safe message —
// never a raw exception type, stack trace, or internal detail, regardless
// of environment. Anything genuinely useful for debugging (the real
// exception, full stack trace) goes to the SERVER-SIDE log via
// _logger.LogError instead — check the backend's console/terminal output
// for that. This keeps the UI honest and safe to show end users while
// still giving developers everything they need, just in the right place.
// -----------------------------------------------------------------------
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = Classify(exception);

        // Only log full details server-side for unexpected (500-class)
        // errors — expected 4xx cases (bad login, not found, a stale
        // cart item) are normal traffic, not incidents.
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }

    private static (HttpStatusCode statusCode, string message) Classify(Exception exception) => exception switch
    {
        NotFoundException => (HttpStatusCode.NotFound, exception.Message),
        ValidationAppException => (HttpStatusCode.BadRequest, exception.Message),
        UnauthorizedAppException => (HttpStatusCode.Unauthorized, exception.Message),
        ForbiddenAppException => (HttpStatusCode.Forbidden, exception.Message),

        // -------------------------------------------------------------
        // ConcurrencyConflictException is Kartly.Application's own,
        // framework-agnostic exception (see Exceptions/ConcurrencyConflictException.cs) —
        // NOT the raw EF Core one. UnitOfWork.SaveChangesAsync() (in
        // Infrastructure) catches the real DbUpdateConcurrencyException at
        // its boundary and rethrows this instead, so CartService/OrderService
        // never need to reference EF Core directly. It only reaches here at
        // all if a service's own retry loop exhausted every attempt — an
        // expected, recoverable condition for a multi-request app, not a
        // bug to hide behind a generic 500. 409 Conflict is the correct
        // HTTP status for it, with a message the UI can act on.
        // -------------------------------------------------------------
        ConcurrencyConflictException => (HttpStatusCode.Conflict, exception.Message),

        // Kept as a defensive fallback in case a raw EF Core exception
        // ever reaches this middleware directly (e.g. a future code path
        // that bypasses UnitOfWork) — should not normally be hit.
        DbUpdateConcurrencyException => (HttpStatusCode.Conflict,
            "This item changed or was removed elsewhere just now. Please refresh and try again."),

        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
    };
}
