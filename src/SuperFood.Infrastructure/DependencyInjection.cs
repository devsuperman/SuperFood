using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SuperFood.Infrastructure.Auth;
using SuperFood.Infrastructure.Identity;
using SuperFood.Infrastructure.Persistence;
using SuperFood.Infrastructure.RealTime;

namespace SuperFood.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSuperFoodInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SuperFoodDbContext>(options => options
            .UseNpgsql(configuration.GetConnectionString("Default"))
            .UseSnakeCaseNamingConvention());

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // ponytail: relaxed so the "food555" default restaurant-owner password is accepted;
                // tighten (and switch owners to a random temp password) if this ever faces the internet.
                options.Password.RequiredLength = 6;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<SuperFoodDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantProvider, HttpCurrentTenantProvider>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddSignalR();
        services.AddScoped<IOrderNotifier, SignalROrderNotifier>();

        return services;
    }
}
