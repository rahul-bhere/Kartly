namespace Kartly.Application.DTOs.Users;

// Used for a user updating their OWN profile. Deliberately does not
// include Role or IsActive — only an admin should change those (see
// AdminUpdateUserDto).
public class UpdateUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
