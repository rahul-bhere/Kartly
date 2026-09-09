namespace Kartly.Application.DTOs.Products;

// Admin-only: used to create a product that is persisted to SQL Server,
// as opposed to the read-only dummy catalog shown to shoppers today.
public class CreateProductDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
}
