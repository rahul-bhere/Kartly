namespace Kartly.Application.DTOs.Users;

// Used by an admin managing another user's account (Admin dashboard).
public class AdminUpdateUserDto
{
    public bool IsActive { get; set; }
    public string Role { get; set; } = string.Empty; // "User" or "Admin"
}
