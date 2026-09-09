namespace Kartly.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
