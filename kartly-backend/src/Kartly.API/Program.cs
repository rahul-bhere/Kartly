using System.Text;
using Kartly.API.Filters;
using Kartly.API.Middleware;
using Kartly.Application;
using Kartly.Infrastructure;
using Kartly.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// 1. Register services from each layer.
//    Notice Program.cs never new()s up a repository or service directly —
//    it just calls each layer's own extension method. This keeps the
//    composition root thin and keeps layer-specific wiring details out
//    of this file.
// -----------------------------------------------------------------------
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<ValidationFilter>();
builder.Services.AddControllers(options =>
{
    // Runs FluentValidation validators (registered in the Application
    // layer) against every incoming DTO automatically — see
    // Filters/ValidationFilter.cs for why this exists.
    options.Filters.AddService<ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();

// ---- Swagger, with a JWT "Authorize" button ----
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Kartly API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste ONLY the token here — Swagger adds the 'Bearer ' prefix for you. Get a token from POST /api/auth/login."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---- CORS: allow the React dev server (and GitHub Pages URL once deployed) ----
const string CorsPolicyName = "KartlyFrontend";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ---- JWT Authentication ----
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];

// A missing OR too-short key both need to stop the app here, at
// startup, with a clear message — not surface as a mysterious 500 on
// the first request that happens to need a token (login/register).
// HMAC-SHA256 requires >= 128 bits (16 bytes); a null-only check lets a
// short-but-non-empty key slip through and fail later instead.
if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 16)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short (needs to be at least 16 bytes / roughly 16+ characters, " +
        "32+ recommended). Set it with: `dotnet user-secrets set \"Jwt:Key\" \"<random string>\"` " +
        "from inside src/Kartly.API — see the backend README's 'Set the JWT signing secret' section. " +
        "Generate one with `openssl rand -base64 48` (or the PowerShell equivalent in the README).");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// -----------------------------------------------------------------------
// 2. Apply migrations + seed the initial admin user/demo data on startup.
//    Convenient for local development; in a real production deployment
//    you would typically run migrations as a separate release step
//    instead of on every app startup.
// -----------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedAsync(db);
}

// -----------------------------------------------------------------------
// 3. Middleware pipeline
// -----------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // browse to /swagger
}

app.UseHttpsRedirection();

// Serves anything in wwwroot/ (including wwwroot/uploads/, where
// ProductsController.UploadImage saves admin-uploaded product photos) as
// static files at the matching URL path, e.g. /uploads/<file>.jpg.
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "uploads"));
app.UseStaticFiles();

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
