using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace web_app_Csharp.Tests;

public sealed class TransferEndpointTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Fact]
    public async Task Transfer_UpdatesBothBalancesAndWritesHistoryForEachAccount()
    {
        var fromAccountId = await CreateAccountAsync("Transfer source");
        var toAccountId = await CreateAccountAsync("Transfer destination");
        var client = factory.CreateClient();
        await client.PostAsJsonAsync($"/api/v1/accounts/{fromAccountId}/deposits", new { amount = 100m });

        var response = await client.PostAsJsonAsync("/api/v1/transfers", new
        {
            fromAccountId,
            toAccountId,
            amount = 60m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(40m, await GetBalanceAsync(fromAccountId));
        Assert.Equal(60m, await GetBalanceAsync(toAccountId));

        var fromHistory = await GetTransactionsAsync(fromAccountId);
        var toHistory = await GetTransactionsAsync(toAccountId);

        var transferOut = Assert.Single(fromHistory, transaction => transaction.Type == "TransferOut");
        Assert.Equal(60m, transferOut.Amount);
        Assert.Equal(40m, transferOut.BalanceAfter);

        var transferIn = Assert.Single(toHistory, transaction => transaction.Type == "TransferIn");
        Assert.Equal(60m, transferIn.Amount);
        Assert.Equal(60m, transferIn.BalanceAfter);
    }

    [Fact]
    public async Task Transfer_WithInsufficientFunds_DoesNotChangeBalancesOrHistory()
    {
        var fromAccountId = await CreateAccountAsync("Insufficient source");
        var toAccountId = await CreateAccountAsync("Insufficient destination");
        var client = factory.CreateClient();
        await client.PostAsJsonAsync($"/api/v1/accounts/{fromAccountId}/deposits", new { amount = 50m });

        var response = await client.PostAsJsonAsync("/api/v1/transfers", new
        {
            fromAccountId,
            toAccountId,
            amount = 75m
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(50m, await GetBalanceAsync(fromAccountId));
        Assert.Equal(0m, await GetBalanceAsync(toAccountId));
        var fromHistory = await GetTransactionsAsync(fromAccountId);
        Assert.Single(fromHistory);
        Assert.Equal("Deposit", fromHistory[0].Type);
        Assert.Empty(await GetTransactionsAsync(toAccountId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Transfer_WithNonPositiveAmount_ReturnsBadRequest(decimal amount)
    {
        var fromAccountId = await CreateAccountAsync("Invalid amount source");
        var toAccountId = await CreateAccountAsync("Invalid amount destination");

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/transfers", new
        {
            fromAccountId,
            toAccountId,
            amount
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_ToSameAccount_ReturnsBadRequest()
    {
        var accountId = await CreateAccountAsync("Same account");

        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/transfers", new
        {
            fromAccountId = accountId,
            toAccountId = accountId,
            amount = 10m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> CreateAccountAsync(string name)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/accounts", new
        {
            ownerId = Guid.NewGuid().ToString(),
            name,
            type = "Checking"
        });
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<decimal> GetBalanceAsync(int accountId)
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/accounts/{accountId}");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("balance").GetDecimal();
    }

    private async Task<List<TransactionSummary>> GetTransactionsAsync(int accountId)
    {
        var response = await factory.CreateClient().GetAsync($"/api/v1/accounts/{accountId}/transactions");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement
            .EnumerateArray()
            .Select(transaction => new TransactionSummary(
                transaction.GetProperty("type").GetString()!,
                transaction.GetProperty("amount").GetDecimal(),
                transaction.GetProperty("balanceAfter").GetDecimal()))
            .ToList();
    }

    private sealed record TransactionSummary(string Type, decimal Amount, decimal BalanceAfter);
}
