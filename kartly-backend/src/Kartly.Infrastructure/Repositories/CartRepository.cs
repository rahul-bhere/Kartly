using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Repositories;

public class CartRepository : GenericRepository<Cart>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context) { }

    public Task<Cart?> GetByUserIdWithItemsAsync(Guid userId) =>
        DbSet
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);
}
