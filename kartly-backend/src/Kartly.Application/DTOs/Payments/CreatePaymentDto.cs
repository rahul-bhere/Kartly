namespace Kartly.Application.DTOs.Payments;

// -----------------------------------------------------------------------
// SIMULATED payment — no real card/PayPal details are collected or sent
// anywhere. This models the shape a real gateway integration (e.g.
// Stripe PaymentIntents) would eventually need, without processing real
// money. See PaymentService for the simulation logic.
// -----------------------------------------------------------------------
public class CreatePaymentDto
{
    public Guid OrderId { get; set; }
    public string Method { get; set; } = string.Empty; // CreditCard/PayPal/CashOnDelivery
}
