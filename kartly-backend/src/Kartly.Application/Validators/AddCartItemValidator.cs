using FluentValidation;
using Kartly.Application.DTOs.Cart;

namespace Kartly.Application.Validators;

public class AddCartItemValidator : AbstractValidator<AddCartItemDto>
{
    public AddCartItemValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);

        RuleFor(x => x)
            .Must(x => x.ProductId.HasValue || !string.IsNullOrWhiteSpace(x.ExternalRef))
            .WithMessage("Either productId (real product) or externalRef (dummy-catalog product) must be provided.")
            .WithName("ProductId");

        When(x => !x.ProductId.HasValue && !string.IsNullOrWhiteSpace(x.ExternalRef), () =>
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required for external (dummy-catalog) items.");
            RuleFor(x => x.UnitPrice).NotNull().GreaterThan(0)
                .WithMessage("unitPrice is required for external (dummy-catalog) items.");
        });
    }
}
