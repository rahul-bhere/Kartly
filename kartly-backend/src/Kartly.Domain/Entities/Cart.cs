using Kartly.Domain.Common;

namespace Kartly.Domain.Entities;

// One cart per user. Kept separate from the User entity so cart-specific
// concerns (items, totals) don't clutter the user's own record.
public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
