using Kartly.Domain.Entities;

namespace Kartly.Application.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<List<Product>> SearchAsync(string? query, string? category);
}
