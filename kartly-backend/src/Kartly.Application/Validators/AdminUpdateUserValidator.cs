using FluentValidation;
using Kartly.Application.DTOs.Users;

namespace Kartly.Application.Validators;

public class AdminUpdateUserValidator : AbstractValidator<AdminUpdateUserDto>
{
    public AdminUpdateUserValidator()
    {
        RuleFor(x => x.Role).NotEmpty().Must(r => r is "User" or "Admin")
            .WithMessage("Role must be either 'User' or 'Admin'.");
    }
}
