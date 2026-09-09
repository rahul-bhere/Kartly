using Kartly.Application.DTOs.Orders;

namespace Kartly.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateFromCartAsync(Guid userId, CreateOrderDto dto);
    Task<List<OrderDto>> GetMyOrdersAsync(Guid userId);
    Task<OrderDto> GetByIdAsync(Guid userId, Guid orderId, bool isAdmin);
    Task<OrderDto> CancelOrderAsync(Guid userId, Guid orderId, bool isAdmin);
    Task<List<OrderDto>> GetAllAsync();
    Task<OrderDto> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto);
}
