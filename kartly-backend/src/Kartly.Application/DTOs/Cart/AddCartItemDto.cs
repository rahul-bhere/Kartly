namespace Kartly.Application.DTOs.Cart;

public class AddCartItemDto
{
    public Guid? ProductId { get; set; }
    public string? ExternalRef { get; set; }
    public string? Title { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Quantity { get; set; } = 1;
}
