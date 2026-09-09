using Kartly.Domain.Common;
using Kartly.Domain.Enums;

namespace Kartly.Domain.Entities;

// Simulated payment record — no real payment gateway is called (see
// PaymentService). Structured so a real gateway (Stripe/Razorpay) can be
// dropped in later without changing the shape of this entity.
public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}
