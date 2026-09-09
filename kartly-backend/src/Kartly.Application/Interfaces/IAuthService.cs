using Kartly.Application.DTOs.Auth;

namespace Kartly.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
