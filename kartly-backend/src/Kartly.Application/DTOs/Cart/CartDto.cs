namespace Kartly.Application.DTOs.Cart;

public class CartDto
{
    public Guid Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.LineTotal);
    public int TotalItems => Items.Sum(i => i.Quantity);
}
