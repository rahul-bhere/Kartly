using FluentValidation;
using Kartly.Application.DTOs.Assistant;

namespace Kartly.Application.Validators;

public class ChatRequestValidator : AbstractValidator<ChatRequestDto>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleForEach(x => x.History).ChildRules(h =>
        {
            h.RuleFor(m => m.Role).Must(r => r is "user" or "assistant")
                .WithMessage("History role must be 'user' or 'assistant'.");
            h.RuleFor(m => m.Content).MaximumLength(4000);
        });
    }
}
