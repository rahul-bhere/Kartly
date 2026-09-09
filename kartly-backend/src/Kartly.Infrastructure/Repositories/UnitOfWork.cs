using Kartly.Application.Exceptions;
using Kartly.Application.Interfaces;
using Kartly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    // -----------------------------------------------------------------------
    // WHY ONLY DbUpdateConcurrencyException IS TRANSLATED (bug fix — an
    // earlier version of this method also caught the much broader
    // DbUpdateException, EF Core's base type for EVERY save failure: FK
    // violations, NOT NULL violations, a stale/out-of-sync schema, bad
    // data — literally anything. That meant ANY unrelated database error
    // during a cart/order save got relabeled as "This item was changed or
    // removed by another request" and silently retried a few times before
    // showing the same misleading message — which is exactly why "add to
    // cart" could fail on the very FIRST click, every time, with a message
    // that describes a rare race condition instead of whatever the real
    // problem was. DbUpdateConcurrencyException specifically means "the
    // row I expected to update/delete didn't match what's actually in the
    // database anymore" — genuinely a concurrency issue, and the ONLY case
    // that should become a ConcurrencyConflictException. Anything else now
    // propagates as a real, uncaught exception, which
    // ExceptionHandlingMiddleware logs in FULL server-side (check the
    // backend console) while still only showing the user a generic 500
    // message — so the actual cause is visible to you, not hidden behind
    // a wrong explanation.
    // -----------------------------------------------------------------------
    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
    }

    public void ResetTracking() => _context.ChangeTracker.Clear();
}
