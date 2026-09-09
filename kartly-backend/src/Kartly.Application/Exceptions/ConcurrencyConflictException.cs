namespace Kartly.Application.Exceptions;

// -----------------------------------------------------------------------
// Represents "this row was changed or removed by another request between
// when we loaded it and when we tried to save" — WITHOUT the Application
// layer needing to know that's backed by EF Core's
// DbUpdateConcurrencyException under the hood. Infrastructure's
// UnitOfWork.SaveChangesAsync() catches the real EF Core exception and
// rethrows THIS one instead — see UnitOfWork.cs.
//
// This is Clean Architecture's dependency rule in practice: Application
// (CartService, OrderService) can catch and react to this (e.g. retry),
// but Kartly.Application.csproj never references
// Microsoft.EntityFrameworkCore at all — only Kartly.Infrastructure does.
// -----------------------------------------------------------------------
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("This item was changed or removed by another request before this one completed.") { }
}
