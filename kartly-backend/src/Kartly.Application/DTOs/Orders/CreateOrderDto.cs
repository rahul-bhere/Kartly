namespace Kartly.Application.DTOs.Orders;

// Checkout request: the order is built server-side from the user's
// CURRENT cart contents (never trust prices/items sent from the
// client) — only the shipping address is taken from this DTO.
public class CreateOrderDto
{
    public string ShippingAddress { get; set; } = string.Empty;
}
