using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using web_app_Csharp.Features.Identity;

namespace web_app_Csharp.Tests;

public sealed record AuthenticatedTestUser(HttpClient Client, string Id);

public static class AuthenticatedTestUsers
{
    public static async Task<AuthenticatedTestUser> CreateAsync(
        BankingApiFactory factory,
        string role = ApplicationRoles.Customer)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var email = $"{Guid.NewGuid():N}@example.test";

        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = Password
        });
        registration.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        var userId = body.RootElement.GetProperty("id").GetString()!;

        if (role == ApplicationRoles.Admin)
        {
            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            Assert.NotNull(user);
            var addRoleResult = await userManager.AddToRoleAsync(user, ApplicationRoles.Admin);
            Assert.True(addRoleResult.Succeeded);
        }

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = Password
        });
        login.EnsureSuccessStatusCode();

        return new AuthenticatedTestUser(client, userId);
    }

    private const string Password = "Portfolio1!";
}
