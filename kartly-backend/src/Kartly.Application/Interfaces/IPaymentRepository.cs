using Kartly.Domain.Entities;

namespace Kartly.Application.Interfaces;

public interface IPaymentRepository : IGenericRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId);
}
