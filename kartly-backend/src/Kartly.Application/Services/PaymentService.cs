using AutoMapper;
using Kartly.Application.DTOs.Payments;
using Kartly.Application.Exceptions;
using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Domain.Enums;

namespace Kartly.Application.Services;

// -----------------------------------------------------------------------
// SIMULATED PAYMENTS ONLY.
// No card numbers, bank details, or real money ever pass through this
// service — it exists to model the shape of a real integration (Stripe,
// Razorpay, PayPal) so swapping one in later means replacing the inside
// of ProcessSimulatedCharge() only, not the API contract used by the
// frontend or the Order/Payment entities.
// -----------------------------------------------------------------------
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PaymentDto> SimulatePaymentAsync(Guid userId, CreatePaymentDto dto)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(dto.OrderId)
            ?? throw new NotFoundException(nameof(Order), dto.OrderId);

        if (order.UserId != userId)
            throw new ForbiddenAppException("You cannot pay for another user's order.");

        if (order.Status != OrderStatus.Pending)
            throw new ValidationAppException($"This order is already '{order.Status}' and cannot be paid again.");

        if (!Enum.TryParse<PaymentMethod>(dto.Method, ignoreCase: true, out var method))
            throw new ValidationAppException(
                $"'{dto.Method}' is not a supported payment method. Use one of: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.");

        var (status, transactionId) = ProcessSimulatedCharge(method, order.TotalAmount);

        var payment = new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = method,
            Status = status,
            TransactionId = transactionId,
            PaidAt = status == PaymentStatus.Completed ? DateTime.UtcNow : null
        };

        await _paymentRepository.AddAsync(payment);

        if (status == PaymentStatus.Completed)
        {
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<PaymentDto>(payment);
    }

    public async Task<PaymentDto> GetByOrderIdAsync(Guid orderId)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId)
            ?? throw new NotFoundException("Payment for order", orderId);

        return _mapper.Map<PaymentDto>(payment);
    }

    // "Charges" nothing real. Cash on Delivery is always pending until
    // delivery; card/PayPal simulate an instant successful charge. Swap
    // this method's body for a real gateway SDK call when you're ready.
    private static (PaymentStatus status, string transactionId) ProcessSimulatedCharge(
        PaymentMethod method, decimal amount)
    {
        var transactionId = $"SIM-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        return method == PaymentMethod.CashOnDelivery
            ? (PaymentStatus.Pending, transactionId)
            : (PaymentStatus.Completed, transactionId);
    }
}
