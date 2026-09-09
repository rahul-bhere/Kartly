using FluentValidation;
using Kartly.Application.DTOs.Payments;

namespace Kartly.Application.Validators;

public class CreatePaymentValidator : AbstractValidator<CreatePaymentDto>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Method).NotEmpty()
            .Must(m => new[] { "CreditCard", "PayPal", "CashOnDelivery" }.Contains(m))
            .WithMessage("Method must be one of: CreditCard, PayPal, CashOnDelivery.");
    }
}
