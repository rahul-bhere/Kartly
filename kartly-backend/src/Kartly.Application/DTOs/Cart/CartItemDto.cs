namespace Kartly.Application.DTOs.Cart;

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public string? ExternalRef { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
