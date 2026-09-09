using Kartly.Domain.Common;

namespace Kartly.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
    public string? ExternalRef { get; set; }

    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public int Quantity { get; set; }
}
