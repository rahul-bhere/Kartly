using Kartly.API.Extensions;
using Kartly.Application.DTOs.Auth;
using Kartly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register
    // Public. Always creates a "User" role account — see AuthService.
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    // POST /api/auth/refresh
    // Exchanges a still-valid refresh token for a brand new access +
    // refresh token pair (rotation — see AuthService.RefreshAsync).
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken);
        return Ok(result);
    }

    // POST /api/auth/logout
    // Requires being logged in, but works for any authenticated user —
    // it only revokes the token passed in, not the whole account.
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return NoContent();
    }
}
