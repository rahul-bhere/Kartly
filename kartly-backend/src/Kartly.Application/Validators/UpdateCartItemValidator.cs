using FluentValidation;
using Kartly.Application.DTOs.Cart;

namespace Kartly.Application.Validators;

public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemDto>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
