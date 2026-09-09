using FluentValidation;
using Kartly.Application.DTOs.Products;

namespace Kartly.Application.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.DiscountPercentage).InclusiveBetween(0, 100);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.Brand).NotEmpty();
        RuleFor(x => x.ThumbnailUrl).NotEmpty().Must(BeAValidUrl).WithMessage("ThumbnailUrl must be a valid URL.");
    }

    private static bool BeAValidUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
