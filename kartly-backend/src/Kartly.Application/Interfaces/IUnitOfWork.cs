namespace Kartly.Application.Interfaces;

// Wraps DbContext.SaveChangesAsync so services commit repository changes
// through one call, and so multiple repository operations in a single
// service method can be committed atomically (one transaction).
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();

    // Clears everything the DbContext is currently tracking, WITHOUT
    // touching the database. Used when retrying after a
    // DbUpdateConcurrencyException: the entities we loaded before are now
    // stale (something else changed the same row in between), so a retry
    // needs to re-query for fresh copies — which EF Core will refuse to
    // do cleanly while it's still tracking the old, conflicting instances
    // under the same primary key. See CartService/OrderService's retry
    // loops for where this gets called.
    void ResetTracking();
}
