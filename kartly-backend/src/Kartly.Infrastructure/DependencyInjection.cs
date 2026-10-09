using Kartly.Application.Interfaces;
using Kartly.Infrastructure.Ai;
using Kartly.Infrastructure.Persistence;
using Kartly.Infrastructure.Repositories;
using Kartly.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kartly.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql =>
                    {
                        sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                        // The free Azure SQL database auto-pauses when idle and takes
                        // up to a minute to resume. Without retries, the first
                        // connection at startup fails and the whole app crashes
                        // (HTTP 500.30). With retries, the app waits for the database.
                        sql.EnableRetryOnFailure(
                            maxRetryCount: 8,
                            maxRetryDelay: TimeSpan.FromSeconds(15),
                            errorNumbersToAdd: null);
                    }));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            // Typed HttpClients with a sane timeout — LLM calls with tool use
            // can take a few seconds, longer than the HttpClient default in
            // some hosting environments.
            services.AddHttpClient<AnthropicLlmClient>(client => client.Timeout = TimeSpan.FromSeconds(30));
            services.AddHttpClient<GroqLlmClient>(client => client.Timeout = TimeSpan.FromSeconds(30));

            // Which LLM provider backs the AI Assistant — see "Llm:Provider"
            // in appsettings.json. Defaults to Groq since it has a genuinely
            // FREE tier (Anthropic's API is paid). Swapping providers is a
            // one-line config change; AssistantService never knows which one
            // is actually running underneath ILlmClient.
            var llmProvider = configuration["Llm:Provider"] ?? "Groq";
            services.AddScoped<ILlmClient>(sp =>
                llmProvider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase)
                    ? sp.GetRequiredService<AnthropicLlmClient>()
                    : sp.GetRequiredService<GroqLlmClient>());

            return services;
        }
    }
}
