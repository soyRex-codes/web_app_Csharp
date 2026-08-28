using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace web_app_Csharp.Tests;

public sealed class AccountEndpointTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Fact]
    public async Task CreateAccount_DerivesOwnerFromAuthenticatedUser()
    {
        var user = await AuthenticatedTestUsers.CreateAsync(factory);
        var response = await user.Client.PostAsJsonAsync("/api/v1/accounts", new
        {
            ownerId = "spoofed-owner",
            name = "Everyday checking",
            type = "Checking"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(user.Id, body.RootElement.GetProperty("ownerId").GetString());
    }

    [Fact]
    public async Task Deposit_WithZeroAmount_ReturnsBadRequest()
    {
        var user = await AuthenticatedTestUsers.CreateAsync(factory);
        var accountId = await CreateAccountAsync(user.Client);

        var response = await user.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/deposits", new { amount = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_AboveBalance_ReturnsConflict()
    {
        var user = await AuthenticatedTestUsers.CreateAsync(factory);
        var accountId = await CreateAccountAsync(user.Client);

        var response = await user.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/withdrawals", new { amount = 1m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<int> CreateAccountAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Test account",
            type = "Checking"
        });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetInt32();
    }
}
