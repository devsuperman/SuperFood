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
                options.Password.RequiredLength = 8;
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
