using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace web_app_Csharp.Tests;

public sealed class ErrorResponseEndpointTests(BankingApiFactory factory) : IClassFixture<BankingApiFactory>
{
    [Fact]
    public async Task ValidationFailure_ReturnsProblemDetails()
    {
        var owner = await AuthenticatedTestUsers.CreateAsync(factory);
        var accountId = await CreateAccountAsync(owner.Client);

        var response = await owner.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/deposits", new { amount = 0m });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/accounts");

        await AssertProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForbiddenRequest_ReturnsProblemDetails()
    {
        var owner = await AuthenticatedTestUsers.CreateAsync(factory);
        var otherCustomer = await AuthenticatedTestUsers.CreateAsync(factory);
        var accountId = await CreateAccountAsync(owner.Client);

        var response = await otherCustomer.Client.GetAsync($"/api/v1/accounts/{accountId}");

        await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MissingAccount_ReturnsProblemDetails()
    {
        var user = await AuthenticatedTestUsers.CreateAsync(factory);

        var response = await user.Client.GetAsync("/api/v1/accounts/2147483647");

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InsufficientFunds_ReturnsProblemDetails()
    {
        var owner = await AuthenticatedTestUsers.CreateAsync(factory);
        var accountId = await CreateAccountAsync(owner.Client);

        var response = await owner.Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/withdrawals", new { amount = 1m });

        await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict);
    }

    private static async Task<int> CreateAccountAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/accounts", new
        {
            name = "Error response account",
            type = "Checking"
        });
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)expectedStatusCode, body.RootElement.GetProperty("status").GetInt32());
        Assert.True(body.RootElement.TryGetProperty("traceId", out _));
    }
}
