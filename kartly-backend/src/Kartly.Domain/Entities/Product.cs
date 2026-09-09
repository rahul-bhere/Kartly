using Kartly.Domain.Common;

namespace Kartly.Domain.Entities;

public class Product : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public double Rating { get; set; }

    // Tracks which admin created the product (nullable: dummy/seeded
    // products created before this feature existed won't have one).
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
