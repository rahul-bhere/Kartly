using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Product>> SearchAsync(string? query, string? category)
    {
        var products = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            products = products.Where(p => p.Title.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            products = products.Where(p => p.Category == category);
        }

        return await products.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }
}
