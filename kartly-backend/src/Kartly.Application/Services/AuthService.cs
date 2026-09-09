using AutoMapper;
using Kartly.Application.DTOs.Auth;
using Kartly.Application.Exceptions;
using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Domain.Enums;

namespace Kartly.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;

    // Refresh tokens live longer than access tokens on purpose — see
    // IJwtTokenService for why the two are split.
    private const int RefreshTokenLifetimeDays = 7;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (await _userRepository.UsernameExistsAsync(request.Username))
            throw new ValidationAppException("That username is already taken.");

        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new ValidationAppException("That email is already registered.");

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.User, // Public registration can NEVER create an admin.
            Cart = new Cart() // Every user gets an empty cart immediately.
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        // Deliberately vague error message — never reveal whether the
        // username or the password was the wrong part.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAppException("Invalid username or password.");

        if (!user.IsActive)
            throw new ForbiddenAppException("This account has been deactivated.");

        return await BuildAuthResponseAsync(user);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (existingToken is null || !existingToken.IsActive)
            throw new UnauthorizedAppException("Refresh token is invalid or expired.");

        // Rotate: revoke the old token and issue a brand new one. This
        // means a stolen refresh token can only be used once before the
        // legitimate user's next refresh silently invalidates it.
        existingToken.RevokedAt = DateTime.UtcNow;

        var user = await _userRepository.GetByIdAsync(existingToken.UserId)
            ?? throw new UnauthorizedAppException("User for this token no longer exists.");

        return await BuildAuthResponseAsync(user);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (existingToken is null) return; // Already gone — logout is idempotent.

        existingToken.RevokedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(User user)
    {
        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            Token = _jwtTokenService.GenerateRefreshTokenValue(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays)
        };
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = expiresAt,
            RefreshToken = refreshToken.Token,
            User = _mapper.Map<Kartly.Application.DTOs.Users.UserDto>(user)
        };
    }
}
