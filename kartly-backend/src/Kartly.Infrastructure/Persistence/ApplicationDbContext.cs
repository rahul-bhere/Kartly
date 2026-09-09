using Kartly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Persistence;

// -----------------------------------------------------------------------
// This is the EF Core "Code First" entry point: the C# entity classes in
// Kartly.Domain are the source of truth, and running
//   dotnet ef migrations add InitialCreate
// generates the SQL Server schema FROM these classes (rather than the
// other way around, which would be "Database First").
// -----------------------------------------------------------------------
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---- User ----
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).HasMaxLength(30);
            entity.Property(u => u.Email).HasMaxLength(256);

            entity.HasOne(u => u.Cart)
                  .WithOne(c => c.User)
                  .HasForeignKey<Cart>(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- Product ----
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.DiscountPercentage).HasColumnType("decimal(5,2)");

            // If the admin who created a product is later deleted, keep
            // the product (set the FK to null) rather than cascading a
            // delete into the catalog.
            entity.HasOne(p => p.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(p => p.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ---- CartItem ----
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.Property(ci => ci.UnitPriceSnapshot).HasColumnType("decimal(18,2)");

            entity.HasOne(ci => ci.Cart)
                  .WithMany(c => c.Items)
                  .HasForeignKey(ci => ci.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Nullable + Restrict: real (admin-created) products can be
            // linked here, but dummy-catalog items (ProductId null,
            // ExternalRef set instead) work exactly the same way without
            // ever needing a matching row in the Products table.
            entity.HasOne(ci => ci.Product)
                  .WithMany(p => p.CartItems)
                  .HasForeignKey(ci => ci.ProductId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- Order / OrderItem ----
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(oi => oi.UnitPriceSnapshot).HasColumnType("decimal(18,2)");

            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany(p => p.OrderItems)
                  .HasForeignKey(oi => oi.ProductId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });


        // ---- Payment ----
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.Order)
                  .WithOne(o => o.Payment)
                  .HasForeignKey<Payment>(p => p.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- RefreshToken ----
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(rt => rt.Token).IsUnique();

            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------------------------------------------------
        // BUG FIX: "Add to cart" was failing with a 409 / DbUpdateConcurrency
        // Exception ("expected to affect 1 row, but affected 0"), even on
        // the very first add for a brand new item.
        //
        // Root cause: BaseEntity.Id is assigned client-side in its property
        // initializer (`Guid.NewGuid()`), so by the time SaveChanges runs, a
        // new CartItem already has a non-empty Id. Nowhere did we tell EF
        // Core that Guid keys are client-generated, so EF Core's default
        // convention (store-generates Guid keys) kicked in. New CartItems
        // are attached via `cart.Items.Add(...)` — i.e. graph fixup off an
        // already-tracked Cart, not an explicit `_context.Add(...)` — and
        // for THAT attachment path, EF Core decides Added vs Unchanged by
        // checking whether the key already has a value. Seeing a non-empty
        // Guid, it assumed the row must already exist, marked the entity
        // Unchanged/Modified, and issued an UPDATE instead of an INSERT.
        // That UPDATE naturally matched 0 rows (the row was never inserted),
        // which EF reports as a concurrency conflict — hence the 409, and
        // hence CartService's built-in retry loop never helping: the
        // failure is deterministic, not an actual race.
        //
        // Fix: explicitly mark every BaseEntity.Id as ValueGeneratedNever()
        // so EF Core always trusts the client-supplied Guid and always
        // treats a graph-attached new entity as Added. This matches how
        // Ids are actually generated in this codebase (in C#, not the DB).
        // -----------------------------------------------------------------
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Kartly.Domain.Common.BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Kartly.Domain.Common.BaseEntity.Id))
                    .ValueGeneratedNever();
            }
        }
    }
}
