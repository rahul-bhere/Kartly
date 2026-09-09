using Kartly.API.Extensions;
using Kartly.Application.DTOs.Orders;
using Kartly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // POST /api/orders/checkout
    // Builds the order from the caller's CURRENT cart server-side.
    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(CreateOrderDto dto)
    {
        var order = await _orderService.CreateFromCartAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // GET /api/orders/mine
    [HttpGet("mine")]
    public async Task<ActionResult<List<OrderDto>>> GetMyOrders()
    {
        var orders = await _orderService.GetMyOrdersAsync(User.GetUserId());
        return Ok(orders);
    }

    // GET /api/orders/{id}
    // A user can view their own order; an admin can view any order.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(User.GetUserId(), id, User.IsAdmin());
        return Ok(order);
    }

    // PUT /api/orders/{id}/cancel
    // The order's OWNER can cancel their own order, and so can an admin.
    [HttpPut("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id)
    {
        var order = await _orderService.CancelOrderAsync(User.GetUserId(), id, User.IsAdmin());
        return Ok(order);
    }

    // GET /api/orders — Admin-only: every order, from every user.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<OrderDto>>> GetAll()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders);
    }

    // PUT /api/orders/{id}/status — Admin-only.
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, UpdateOrderStatusDto dto)
    {
        var order = await _orderService.UpdateStatusAsync(id, dto);
        return Ok(order);
    }
}
