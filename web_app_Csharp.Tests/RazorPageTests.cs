using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        Assert.Contains("Open an account", content);
        Assert.Contains("__RequestVerificationToken", content);
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

    [Fact]
    public async Task Customer_CanCompleteBankingWorkflowThroughRazorForms()
    {
        var user = await AuthenticatedTestUsers.CreateAsync(factory);

        await PostFormAsync(user.Client, "/accounts?handler=Create", new Dictionary<string, string>
        {
            ["name"] = "Everyday checking",
            ["type"] = "Checking"
        });
        await PostFormAsync(user.Client, "/accounts?handler=Create", new Dictionary<string, string>
        {
            ["name"] = "Savings",
            ["type"] = "Savings"
        });

        var accounts = await GetAccountsAsync(user.Client);
        var checking = Assert.Single(accounts, account => account.Name == "Everyday checking");
        var savings = Assert.Single(accounts, account => account.Name == "Savings");

        await PostFormAsync(user.Client, $"/accounts/{checking.Id}?handler=Deposit", new Dictionary<string, string>
        {
            ["amount"] = "100.00"
        });
        await PostFormAsync(user.Client, $"/accounts/{checking.Id}?handler=Transfer", new Dictionary<string, string>
        {
            ["toAccountId"] = savings.Id.ToString(),
            ["amount"] = "40.00"
        });

        var detail = await user.Client.GetAsync($"/accounts/{checking.Id}");
        detail.EnsureSuccessStatusCode();
        var content = await detail.Content.ReadAsStringAsync();
        Assert.Contains("$60.00", content);
        Assert.Contains("TransferOut", content);
        Assert.Contains("Deposit", content);

        var insufficientWithdrawal = await PostFormAsync(
            user.Client,
            $"/accounts/{checking.Id}?handler=Withdraw",
            new Dictionary<string, string> { ["amount"] = "61.00" });
        Assert.Contains("The account has insufficient funds.", await insufficientWithdrawal.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RazorMoneyForms_RejectPostsWithoutAnAntiForgeryToken()
    {
        var user = await AuthenticatedTestUsers.CreateAsync(factory);

        var response = await user.Client.PostAsync("/accounts?handler=Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "Untrusted request",
            ["type"] = "Checking"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string path,
        IDictionary<string, string> values)
    {
        var page = await client.GetAsync(path.Split('?')[0]);
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();

        values["__RequestVerificationToken"] = ExtractAntiForgeryToken(html);
        var response = await client.PostAsync(path, new FormUrlEncodedContent(values));
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var tokenMatch = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(tokenMatch.Success, "The page did not render an anti-forgery token.");
        return WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value);
    }

    private static async Task<List<AccountSummary>> GetAccountsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/accounts");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement
            .EnumerateArray()
            .Select(account => new AccountSummary(
                account.GetProperty("id").GetInt32(),
                account.GetProperty("name").GetString()!))
            .ToList();
    }

    private sealed record AccountSummary(int Id, string Name);
}
