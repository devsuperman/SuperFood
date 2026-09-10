using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SuperFood.Api.Common;
using SuperFood.Domain;
using SuperFood.Infrastructure;
using SuperFood.Infrastructure.Auth;
using SuperFood.Infrastructure.Persistence;
using SuperFood.Infrastructure.RealTime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSuperFoodInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // SignalR sends the token via the query string, not a header (docs/tech-stack.md §7).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("platform_admin", policy => policy.RequireClaim("is_platform_admin", "true"))
    .AddPolicy(Permissions.RestaurantManage, policy => policy.RequireClaim("permission", Permissions.RestaurantManage))
    .AddPolicy(Permissions.UsersManage, policy => policy.RequireClaim("permission", Permissions.UsersManage))
    .AddPolicy(Permissions.MenuManage, policy => policy.RequireClaim("permission", Permissions.MenuManage))
    .AddPolicy(Permissions.TablesManage, policy => policy.RequireClaim("permission", Permissions.TablesManage))
    .AddPolicy(Permissions.OrdersManage, policy => policy.RequireClaim("permission", Permissions.OrdersManage))
    .AddPolicy(Permissions.KitchenManage, policy => policy.RequireClaim("permission", Permissions.KitchenManage));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar/v1

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SuperFoodDbContext>();
    await db.Database.MigrateAsync();
    await PlatformAdminSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseExceptionHandler();
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<OrdersHub>("/hubs/orders");
app.MapEndpoints();

app.Run();

// Exposed for WebApplicationFactory in SuperFood.IntegrationTests.
public partial class Program;
