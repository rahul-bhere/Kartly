using AutoMapper;
using Kartly.Application.DTOs.Orders;
using Kartly.Application.Exceptions;
using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Domain.Enums;

namespace Kartly.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    // Same reasoning as CartService.MaxAttempts — checkout touches both
    // Cart and Order rows in one save, so it's exposed to the same
    // transient race (e.g. a stray cart edit landing mid-checkout).
    private const int MaxAttempts = 3;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderDto> CreateFromCartAsync(Guid userId, CreateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ShippingAddress))
            throw new ValidationAppException("A shipping address is required.");

        var orderId = await RetryOnConcurrencyConflict(async () =>
        {
            var cart = await _cartRepository.GetByUserIdWithItemsAsync(userId);

            if (cart is null || cart.Items.Count == 0)
                throw new ValidationAppException("Your cart is empty — add items before checking out.");

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = dto.ShippingAddress,
                Status = OrderStatus.Pending,
                Items = cart.Items.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    ExternalRef = ci.ExternalRef,
                    ProductNameSnapshot = ci.TitleSnapshot,
                    UnitPriceSnapshot = ci.UnitPriceSnapshot,
                    Quantity = ci.Quantity
                }).ToList()
            };
            order.TotalAmount = order.Items.Sum(i => i.UnitPriceSnapshot * i.Quantity);

            await _orderRepository.AddAsync(order);
            cart.Items.Clear();

            await _unitOfWork.SaveChangesAsync();
            return order.Id;
        });

        var created = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        return _mapper.Map<OrderDto>(created);
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(Guid userId)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId);
        return _mapper.Map<List<OrderDto>>(orders);
    }

    public async Task<OrderDto> GetByIdAsync(Guid userId, Guid orderId, bool isAdmin)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId)
            ?? throw new NotFoundException(nameof(Order), orderId);

        if (!isAdmin && order.UserId != userId)
            throw new ForbiddenAppException("You do not have access to this order.");

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<List<OrderDto>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllWithDetailsAsync();
        return _mapper.Map<List<OrderDto>>(orders);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto)
    {
        if (!Enum.TryParse<OrderStatus>(dto.Status, ignoreCase: true, out var newStatus))
            throw new ValidationAppException(
                $"'{dto.Status}' is not a valid order status. Use one of: {string.Join(", ", Enum.GetNames<OrderStatus>())}.");

        await RetryOnConcurrencyConflict(async () =>
        {
            var order = await _orderRepository.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(nameof(Order), orderId);

            order.Status = newStatus;
            if (dto.CustomStatusLabel is not null)
                order.CustomStatusLabel = string.IsNullOrWhiteSpace(dto.CustomStatusLabel) ? null : dto.CustomStatusLabel;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return true;
        });

        var updated = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        return _mapper.Map<OrderDto>(updated);
    }

    public async Task<OrderDto> CancelOrderAsync(Guid userId, Guid orderId, bool isAdmin)
    {
        await RetryOnConcurrencyConflict(async () =>
        {
            var order = await _orderRepository.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException(nameof(Order), orderId);

            if (!isAdmin && order.UserId != userId)
                throw new ForbiddenAppException("You do not have access to this order.");

            if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
                throw new ValidationAppException($"An order that is already '{order.Status}' cannot be cancelled.");

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync();
            return true;
        });

        var cancelled = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        return _mapper.Map<OrderDto>(cancelled);
    }

    private async Task<T> RetryOnConcurrencyConflict<T>(Func<Task<T>> operation)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                _unitOfWork.ResetTracking();
            }
        }

        return await operation();
    }
}
