using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Order>> GetByUserIdAsync(Guid userId) =>
        await DbSet
            .Include(o => o.Items)
            .Include(o => o.User)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

    public Task<Order?> GetByIdWithDetailsAsync(Guid orderId) =>
        DbSet
            .Include(o => o.Items)
            .Include(o => o.User)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

    public async Task<List<Order>> GetAllWithDetailsAsync() =>
        await DbSet
            .Include(o => o.Items)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
}
