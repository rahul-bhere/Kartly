using Kartly.Domain.Entities;

namespace Kartly.Application.Interfaces;

public interface ICartRepository : IGenericRepository<Cart>
{
    // Includes Items + Product navigation loaded, ready to map to a DTO.
    Task<Cart?> GetByUserIdWithItemsAsync(Guid userId);
}
