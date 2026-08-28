using Microsoft.AspNetCore.Identity;

namespace web_app_Csharp.Features.Identity;

public static class IdentityDataSeeder
{
    public static async Task EnsureRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in ApplicationRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            EnsureSucceeded(result, $"create the {role} role");
        }
    }

    public static async Task SeedDevelopmentAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var email = configuration["Identity:BootstrapAdmin:Email"]?.Trim();
        var password = configuration["Identity:BootstrapAdmin:Password"];

        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(password))
        {
            return;
        }

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Both Identity:BootstrapAdmin:Email and Identity:BootstrapAdmin:Password are required to seed a development admin.");
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, "create the development admin");
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
            EnsureSucceeded(addRoleResult, "assign the development admin role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {operation}: {errors}");
    }
}
