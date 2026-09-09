using FluentValidation;
using Kartly.Application.DTOs.Orders;

namespace Kartly.Application.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.ShippingAddress).NotEmpty().MinimumLength(10).MaximumLength(300);
    }
}
