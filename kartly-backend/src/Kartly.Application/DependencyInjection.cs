using FluentValidation;
using Kartly.Application.Interfaces;
using Kartly.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kartly.Application;

// -----------------------------------------------------------------------
// Called once from Kartly.API's Program.cs (AddApplicationServices()).
// Keeping this here means Program.cs doesn't need to know the internal
// details of what the Application layer contains.
// -----------------------------------------------------------------------
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAssistantService, AssistantService>();

        return services;
    }
}
