using Kartly.Application.DTOs.Cart;

namespace Kartly.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto);
    Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto);
    Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId);
    Task ClearCartAsync(Guid userId);
}
