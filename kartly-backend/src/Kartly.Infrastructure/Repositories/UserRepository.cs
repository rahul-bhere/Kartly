using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public Task<User?> GetByUsernameAsync(string username) =>
        DbSet.FirstOrDefaultAsync(u => u.Username == username);

    public Task<User?> GetByEmailAsync(string email) =>
        DbSet.FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> UsernameExistsAsync(string username) =>
        DbSet.AnyAsync(u => u.Username == username);

    public Task<bool> EmailExistsAsync(string email) =>
        DbSet.AnyAsync(u => u.Email == email);
}
