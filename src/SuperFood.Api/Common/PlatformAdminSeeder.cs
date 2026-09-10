using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SuperFood.Infrastructure.Identity;

namespace SuperFood.Api.Common;

/// <summary>
/// Bootstraps the first platform_admin (EPIC-01) from configuration, since no
/// self-registration flow exists — every other account is created by one.
/// </summary>
public static class PlatformAdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration["PlatformAdmin:Email"];
        var password = configuration["PlatformAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Platform Admin",
            IsPlatformAdmin = true,
            IsActive = true,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(admin, password);
    }
}
