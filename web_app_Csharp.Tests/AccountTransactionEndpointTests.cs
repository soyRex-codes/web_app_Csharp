using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace web_app_Csharp.Tests;

public sealed class AccountTransactionEndpointTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Fact]
    public async Task Deposit_CreatesHistoryEntryWithResultingBalance()
    {
        var accountId = await CreateAccountAsync();
        var client = factory.CreateClient();

        var deposit = await client.PostAsJsonAsync($"/api/v1/accounts/{accountId}/deposits", new { amount = 25m });
        Assert.Equal(HttpStatusCode.OK, deposit.StatusCode);

        var history = await client.GetAsync($"/api/v1/accounts/{accountId}/transactions");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);

        using var body = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        var transaction = body.RootElement[0];
        Assert.Equal("Deposit", transaction.GetProperty("type").GetString());
        Assert.Equal(25m, transaction.GetProperty("amount").GetDecimal());
        Assert.Equal(25m, transaction.GetProperty("balanceAfter").GetDecimal());
    }

    [Fact]
    public async Task Transactions_AreReturnedNewestFirst()
    {
        var accountId = await CreateAccountAsync();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync($"/api/v1/accounts/{accountId}/deposits", new { amount = 25m });
        await client.PostAsJsonAsync($"/api/v1/accounts/{accountId}/withdrawals", new { amount = 5m });

        var response = await client.GetAsync($"/api/v1/accounts/{accountId}/transactions");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Withdrawal", body.RootElement[0].GetProperty("type").GetString());
        Assert.Equal("Deposit", body.RootElement[1].GetProperty("type").GetString());
    }

    private async Task<int> CreateAccountAsync()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/accounts", new
        {
            ownerId = Guid.NewGuid().ToString(),
            name = "Transaction test account",
            type = "Checking"
        });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetInt32();
    }
}
