using Kartly.API.Extensions;
using Kartly.Application.DTOs.Users;
using Kartly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // GET /api/users/me
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var user = await _userService.GetByIdAsync(User.GetUserId());
        return Ok(user);
    }

    // PUT /api/users/me
    // A user updating their OWN profile — cannot change their own role
    // or active status this way (see UpdateUserDto).
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMyProfile(UpdateUserDto dto)
    {
        var updated = await _userService.UpdateProfileAsync(User.GetUserId(), dto);
        return Ok(updated);
    }

    // POST /api/users/me/change-password
    // Available to ANY authenticated user (shopper or admin) for their
    // OWN account — requires the current password.
    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordDto dto)
    {
        await _userService.ChangePasswordAsync(User.GetUserId(), dto);
        return NoContent();
    }

    // GET /api/users — Admin-only: the Admin Dashboard's user list.
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    // PUT /api/users/{id} — Admin-only: change another user's role/active status.
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> AdminUpdateUser(Guid id, AdminUpdateUserDto dto)
    {
        var updated = await _userService.AdminUpdateUserAsync(id, dto);
        return Ok(updated);
    }

    // DELETE /api/users/{id} — Admin-only.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}
