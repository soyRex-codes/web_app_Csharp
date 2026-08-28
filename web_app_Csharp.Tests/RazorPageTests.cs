using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using web_app_Csharp.Features.Identity;

namespace web_app_Csharp.Tests;

public sealed class RazorPageTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Theory]
    [InlineData("/register", "Create your user account")]
    [InlineData("/login", "Sign in")]
    public async Task PublicPages_RenderExpectedContent(string path, string expectedText)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.EnsureSuccessStatusCode();
        Assert.Contains(expectedText, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MyAccountsPage_RequiresSignIn()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/accounts");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/login", response.Headers.Location.AbsolutePath);
    }

    [Fact]
    public async Task SignedInCustomer_CanOpenMyAccountsPage()
    {
        var user = await AuthenticatedTestUsers.CreateAsync(factory);

        var response = await user.Client.GetAsync("/accounts");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("My accounts", content);
        Assert.Contains("data-accounts-table", content);
        Assert.Contains("/js/banking-pages.js", content);
    }

    [Fact]
    public async Task AdminPage_IsDeniedToCustomersAndAvailableToAdmins()
    {
        var customer = await AuthenticatedTestUsers.CreateAsync(factory);
        var admin = await AuthenticatedTestUsers.CreateAsync(factory, ApplicationRoles.Admin);

        var customerResponse = await customer.Client.GetAsync("/admin/accounts");
        var adminResponse = await admin.Client.GetAsync("/admin/accounts");

        customerResponse.EnsureSuccessStatusCode();
        Assert.Contains("You do not have access to this page.", await customerResponse.Content.ReadAsStringAsync());

        adminResponse.EnsureSuccessStatusCode();
        Assert.Contains("All accounts", await adminResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ShellAssets_AreServed()
    {
        var response = await factory.CreateClient().GetAsync("/css/site.css");

        response.EnsureSuccessStatusCode();
        Assert.Contains(":root", await response.Content.ReadAsStringAsync());
    }
}
