using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Repositories;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context) { }

    public Task<Payment?> GetByOrderIdAsync(Guid orderId) =>
        DbSet.FirstOrDefaultAsync(p => p.OrderId == orderId);
}
