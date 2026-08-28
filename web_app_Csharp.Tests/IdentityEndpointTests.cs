using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using web_app_Csharp.Features.Identity;

namespace web_app_Csharp.Tests;

public sealed class IdentityEndpointTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Fact]
    public async Task Register_CreatesCustomerUser()
    {
        var email = NewEmail();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
        Assert.Equal(ApplicationRoles.Customer, body.RootElement.GetProperty("role").GetString());

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, ApplicationRoles.Customer));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ThenLogout_ManagesAuthenticationCookie()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers, header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

        var logout = await client.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "WrongPassword1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentAdminSeed_WithExplicitConfiguration_CreatesAdmin()
    {
        var email = NewEmail();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:BootstrapAdmin:Email"] = email,
                ["Identity:BootstrapAdmin:Password"] = ValidPassword
            })
            .Build();

        using var scope = factory.Services.CreateScope();
        await IdentityDataSeeder.SeedDevelopmentAdminAsync(scope.ServiceProvider, configuration);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, ApplicationRoles.Admin));
    }

    private async Task RegisterAsync(string email)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = ValidPassword
        });
        response.EnsureSuccessStatusCode();
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.test";

    private const string ValidPassword = "Portfolio1!";
}
