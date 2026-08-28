using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using web_app_Csharp.Features.Identity;

namespace web_app_Csharp.Tests;

public sealed class AccountAuthorizationEndpointTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Fact]
    public async Task Accounts_AllowOwnersAndAdminsButForbidOtherCustomers()
    {
        var owner = await AuthenticatedTestUsers.CreateAsync(factory);
        var otherCustomer = await AuthenticatedTestUsers.CreateAsync(factory);
        var admin = await AuthenticatedTestUsers.CreateAsync(factory, ApplicationRoles.Admin);
        var accountId = await CreateAccountAsync(owner.Client, "Owner account");

        Assert.Equal(HttpStatusCode.OK, (await owner.Client.GetAsync($"/api/v1/accounts/{accountId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherCustomer.Client.GetAsync($"/api/v1/accounts/{accountId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.GetAsync($"/api/v1/accounts/{accountId}")).StatusCode);

        var otherAccounts = await otherCustomer.Client.GetAsync("/api/v1/accounts");
        otherAccounts.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await otherAccounts.Content.ReadAsStringAsync());
        Assert.Empty(body.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task MoneyOperations_AreForbiddenToOtherCustomersAndAllowedToAdmins()
    {
        var owner = await AuthenticatedTestUsers.CreateAsync(factory);
        var otherCustomer = await AuthenticatedTestUsers.CreateAsync(factory);
        var admin = await AuthenticatedTestUsers.CreateAsync(factory, ApplicationRoles.Admin);
        var accountId = await CreateAccountAsync(owner.Client, "Protected account");

        var deposit = await otherCustomer.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/deposits", new { amount = 20m });
        var withdrawal = await otherCustomer.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/withdrawals", new { amount = 10m });

        Assert.Equal(HttpStatusCode.Forbidden, deposit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, withdrawal.StatusCode);
        Assert.Equal(0m, await GetBalanceAsync(owner.Client, accountId));

        var adminDeposit = await admin.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/deposits", new { amount = 20m });
        Assert.Equal(HttpStatusCode.OK, adminDeposit.StatusCode);
        Assert.Equal(20m, await GetBalanceAsync(owner.Client, accountId));
    }

    [Fact]
    public async Task TransactionHistory_IsForbiddenToOtherCustomersAndAvailableToAdmins()
    {
        var owner = await AuthenticatedTestUsers.CreateAsync(factory);
        var otherCustomer = await AuthenticatedTestUsers.CreateAsync(factory);
        var admin = await AuthenticatedTestUsers.CreateAsync(factory, ApplicationRoles.Admin);
        var accountId = await CreateAccountAsync(owner.Client, "History account");
        await owner.Client.PostAsJsonAsync($"/api/v1/accounts/{accountId}/deposits", new { amount = 20m });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await otherCustomer.Client.GetAsync($"/api/v1/accounts/{accountId}/transactions")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.Client.GetAsync($"/api/v1/accounts/{accountId}/transactions")).StatusCode);
    }

    [Fact]
    public async Task Transfers_RequireCustomerOwnershipOfBothAccountsButAllowAdmins()
    {
        var sourceOwner = await AuthenticatedTestUsers.CreateAsync(factory);
        var destinationOwner = await AuthenticatedTestUsers.CreateAsync(factory);
        var admin = await AuthenticatedTestUsers.CreateAsync(factory, ApplicationRoles.Admin);
        var sourceAccountId = await CreateAccountAsync(sourceOwner.Client, "Source account");
        var destinationAccountId = await CreateAccountAsync(destinationOwner.Client, "Destination account");
        await sourceOwner.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{sourceAccountId}/deposits", new { amount = 100m });

        var customerTransfer = await sourceOwner.Client.PostAsJsonAsync("/api/v1/transfers", new
        {
            fromAccountId = sourceAccountId,
            toAccountId = destinationAccountId,
            amount = 60m
        });

        Assert.Equal(HttpStatusCode.Forbidden, customerTransfer.StatusCode);
        Assert.Equal(100m, await GetBalanceAsync(sourceOwner.Client, sourceAccountId));
        Assert.Equal(0m, await GetBalanceAsync(destinationOwner.Client, destinationAccountId));
        Assert.Equal(1, await GetTransactionCountAsync(sourceOwner.Client, sourceAccountId));
        Assert.Equal(0, await GetTransactionCountAsync(destinationOwner.Client, destinationAccountId));

        var adminTransfer = await admin.Client.PostAsJsonAsync("/api/v1/transfers", new
        {
            fromAccountId = sourceAccountId,
            toAccountId = destinationAccountId,
            amount = 60m
        });

        Assert.Equal(HttpStatusCode.OK, adminTransfer.StatusCode);
        Assert.Equal(40m, await GetBalanceAsync(sourceOwner.Client, sourceAccountId));
        Assert.Equal(60m, await GetBalanceAsync(destinationOwner.Client, destinationAccountId));
        Assert.Equal(2, await GetTransactionCountAsync(sourceOwner.Client, sourceAccountId));
        Assert.Equal(1, await GetTransactionCountAsync(destinationOwner.Client, destinationAccountId));
    }

    [Fact]
    public async Task Accounts_RequireAuthentication()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<int> CreateAccountAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name,
            type = "Checking"
        });
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<decimal> GetBalanceAsync(HttpClient client, int accountId)
    {
        var response = await client.GetAsync($"/api/v1/accounts/{accountId}");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("balance").GetDecimal();
    }

    private static async Task<int> GetTransactionCountAsync(HttpClient client, int accountId)
    {
        var response = await client.GetAsync($"/api/v1/accounts/{accountId}/transactions");
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetArrayLength();
    }
}
