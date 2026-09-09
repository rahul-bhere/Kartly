using Kartly.Domain.Common;

namespace Kartly.Domain.Entities;

// A cart item can point at a REAL product (created via the Admin
// Dashboard, ProductId set, FK to Products table) OR a DUMMY-catalog
// product (ProductId is null, ExternalRef holds something like
// "dummy-42"). Title/UnitPrice/ThumbnailUrl are captured directly from
// the frontend at add-to-cart time — this is what lets a product that
// only exists in the public dummy API still be added to a REAL,
// persisted cart in SQL Server.
public class CartItem : BaseEntity
{
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? ExternalRef { get; set; }

    public string TitleSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public string? ThumbnailUrl { get; set; }

    public int Quantity { get; set; }
}
