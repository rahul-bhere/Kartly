using Kartly.Domain.Common;
using Kartly.Domain.Enums;

namespace Kartly.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Optional admin-set free text shown to the shopper INSTEAD of the
    // enum name (e.g. "Out for delivery today"), while Status stays a
    // fixed enum for any business logic that depends on it.
    public string? CustomStatusLabel { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}
