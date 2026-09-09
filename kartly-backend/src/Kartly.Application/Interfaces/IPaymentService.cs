using Kartly.Application.DTOs.Payments;

namespace Kartly.Application.Interfaces;

public interface IPaymentService
{
    // Simulates charging a payment method for an order's total. See
    // PaymentService for exactly what "simulated" means here.
    Task<PaymentDto> SimulatePaymentAsync(Guid userId, CreatePaymentDto dto);
    Task<PaymentDto> GetByOrderIdAsync(Guid orderId);
}
