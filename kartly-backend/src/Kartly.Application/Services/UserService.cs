using AutoMapper;
using Kartly.Application.DTOs.Users;
using Kartly.Application.Exceptions;
using Kartly.Application.Interfaces;
using Kartly.Domain.Entities;
using Kartly.Domain.Enums;

namespace Kartly.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(User), id);
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateProfileAsync(Guid userId, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase)
            && await _userRepository.EmailExistsAsync(dto.Email))
        {
            throw new ValidationAppException("That email is already in use by another account.");
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!_passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new ValidationAppException("Current password is incorrect.");

        if (dto.NewPassword.Length < 8)
            throw new ValidationAppException("New password must be at least 8 characters long.");

        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<UserDto> AdminUpdateUserAsync(Guid userId, AdminUpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role))
            throw new ValidationAppException($"'{dto.Role}' is not a valid role. Use 'User' or 'Admin'.");

        user.Role = role;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException(nameof(User), userId);

        _userRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync();
    }
}
