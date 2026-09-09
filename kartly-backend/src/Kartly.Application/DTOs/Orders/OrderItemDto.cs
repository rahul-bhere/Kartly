namespace Kartly.Application.DTOs.Orders;

public class OrderItemDto
{
    public Guid? ProductId { get; set; }
    public string? ExternalRef { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
