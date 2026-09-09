using Kartly.API.Extensions;
using Kartly.Application.DTOs.Cart;
using Kartly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Every action here requires a logged-in user; there's no such thing as an anonymous cart on the backend.
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // GET /api/cart
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetMyCart()
    {
        var cart = await _cartService.GetCartAsync(User.GetUserId());
        return Ok(cart);
    }

    // POST /api/cart/items
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemDto dto)
    {
        var cart = await _cartService.AddItemAsync(User.GetUserId(), dto);
        return Ok(cart);
    }

    // PUT /api/cart/items/{cartItemId}
    [HttpPut("items/{cartItemId:guid}")]
    public async Task<ActionResult<CartDto>> UpdateItem(Guid cartItemId, UpdateCartItemDto dto)
    {
        var cart = await _cartService.UpdateItemAsync(User.GetUserId(), cartItemId, dto);
        return Ok(cart);
    }

    // DELETE /api/cart/items/{cartItemId}
    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid cartItemId)
    {
        var cart = await _cartService.RemoveItemAsync(User.GetUserId(), cartItemId);
        return Ok(cart);
    }

    // DELETE /api/cart
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync(User.GetUserId());
        return NoContent();
    }
}
