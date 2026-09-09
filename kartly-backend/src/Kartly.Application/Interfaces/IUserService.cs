using Kartly.Application.DTOs.Users;

namespace Kartly.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> UpdateProfileAsync(Guid userId, UpdateUserDto dto);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<List<UserDto>> GetAllAsync();
    Task<UserDto> AdminUpdateUserAsync(Guid userId, AdminUpdateUserDto dto);
    Task DeleteUserAsync(Guid userId);
}
