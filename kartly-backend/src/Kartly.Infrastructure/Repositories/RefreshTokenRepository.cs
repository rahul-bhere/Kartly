using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kartly.Infrastructure.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context) { }

    public Task<RefreshToken?> GetByTokenAsync(string token) =>
        DbSet.FirstOrDefaultAsync(rt => rt.Token == token);
}
