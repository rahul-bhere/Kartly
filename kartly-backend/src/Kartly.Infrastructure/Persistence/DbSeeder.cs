using Kartly.Domain.Entities;
using Kartly.Domain.Enums;
using Kartly.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Persistence;

// -----------------------------------------------------------------------
// Runs once at startup (see Program.cs). Without this, there would be NO
// way to log in as an admin the first time, since public registration
// can never create one (see AuthService.RegisterAsync).
// -----------------------------------------------------------------------
public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var hasher = new BCryptPasswordHasher();

            var admin = new User
            {
                FirstName = "Kartly",
                LastName = "Admin",
                Username = "admin",
                Email = "admin@kartly.local",
                // CHANGE THIS PASSWORD after first login in any real
                // deployment. It exists only so there's a way in.
                PasswordHash = hasher.Hash("Admin@123"),
                Role = UserRole.Admin,
                Cart = new Cart()
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
