using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kartly.API.Filters;

// -----------------------------------------------------------------------
// FluentValidation's automatic MVC integration was discontinued, so this
// small filter does the equivalent by hand: for every action argument
// that has a registered IValidator<T> (see Application/DependencyInjection.cs
// -> AddValidatorsFromAssembly), run it BEFORE the action executes and
// short-circuit with 400 + field errors if it fails.
//
// This means every controller action stays free of manual
// "if (!ModelState.IsValid) ..." boilerplate — validation happens in one
// place for the whole API.
// -----------------------------------------------------------------------
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
                continue; // No validator registered for this DTO type — nothing to check.

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { errors });
                return;
            }
        }

        await next();
    }
}
