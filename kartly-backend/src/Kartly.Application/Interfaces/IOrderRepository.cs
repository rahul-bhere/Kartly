using Kartly.Domain.Entities;

namespace Kartly.Application.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<List<Order>> GetByUserIdAsync(Guid userId);
    Task<Order?> GetByIdWithDetailsAsync(Guid orderId);
    Task<List<Order>> GetAllWithDetailsAsync();
}
