using FluentValidation;
using Kartly.Application.DTOs.Orders;

namespace Kartly.Application.Validators;

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => new[] { "Pending", "Paid", "Shipped", "Delivered", "Cancelled" }.Contains(s))
            .WithMessage("Status must be one of: Pending, Paid, Shipped, Delivered, Cancelled.");
        RuleFor(x => x.CustomStatusLabel).MaximumLength(200);
    }
}
