using System.Linq.Expressions;

namespace Kartly.Application.Interfaces;

// -----------------------------------------------------------------------
// A generic repository covers the common CRUD shape shared by every
// entity, so we don't repeat GetById/Add/Update/Delete five times.
// Entity-specific repositories (IProductRepository, etc.) extend this
// with queries that need more than a generic "find by id".
// -----------------------------------------------------------------------
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
