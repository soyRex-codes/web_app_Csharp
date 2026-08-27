using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace web_app_Csharp.Tests;

public sealed class AccountEndpointTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Fact]
    public async Task CreateAccount_ReturnsCreatedResponse()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/accounts", new
        {
            ownerId = "user-1",
            name = "Everyday checking",
            type = "Checking"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("user-1", body.RootElement.GetProperty("ownerId").GetString());
    }

    [Fact]
    public async Task Deposit_WithZeroAmount_ReturnsBadRequest()
    {
        var accountId = await CreateAccountAsync();

        var response = await factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/deposits", new { amount = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_AboveBalance_ReturnsConflict()
    {
        var accountId = await CreateAccountAsync();

        var response = await factory.CreateClient().PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/withdrawals", new { amount = 1m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<int> CreateAccountAsync()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/accounts", new
        {
            ownerId = Guid.NewGuid().ToString(),
            name = "Test account",
            type = "Checking"
        });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetInt32();
    }
}
