using System.Security.Claims;

namespace web_app_Csharp.Features.Accounts;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapGet("", GetAccounts);
        group.MapGet("/{id:int}", GetAccount);
        group.MapGet("/{id:int}/transactions", GetTransactions);
        group.MapPost("", CreateAccount);
        group.MapPost("/{id:int}/deposits", Deposit);
        group.MapPost("/{id:int}/withdrawals", Withdraw);

        return endpoints;
    }

    private static async Task<IResult> GetAccounts(
        AccountOperationsService operations,
        ClaimsPrincipal user,
        CancellationToken cancellationToken) =>
        (await operations.GetAccountsAsync(user, cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAccount(
        int id,
        AccountOperationsService operations,
        ClaimsPrincipal user,
        CancellationToken cancellationToken) =>
        (await operations.GetAccountAsync(id, user, cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetTransactions(
        int id,
        AccountOperationsService operations,
        ClaimsPrincipal user,
        CancellationToken cancellationToken) =>
        (await operations.GetTransactionsAsync(id, user, cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateAccount(
        CreateAccountRequest request,
        AccountOperationsService operations,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var result = await operations.CreateAsync(request, user, cancellationToken);
        return result.ToHttpResult(account => TypedResults.Created($"/api/v1/accounts/{account.Id}", account));
    }

    private static async Task<IResult> Deposit(
        int id,
        AccountTransactionRequest request,
        AccountOperationsService operations,
        ClaimsPrincipal user,
        CancellationToken cancellationToken) =>
        (await operations.DepositAsync(id, request, user, cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> Withdraw(
        int id,
        AccountTransactionRequest request,
        AccountOperationsService operations,
        ClaimsPrincipal user,
        CancellationToken cancellationToken) =>
        (await operations.WithdrawAsync(id, request, user, cancellationToken)).ToHttpResult(TypedResults.Ok);
}

internal static class AccountOperationResultHttpExtensions
{
    public static IResult ToHttpResult<T>(this AccountOperationResult<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var error = result.Error!;
        return error.Kind switch
        {
            AccountOperationErrorKind.NotFound => TypedResults.NotFound(),
            AccountOperationErrorKind.Forbidden => TypedResults.Forbid(),
            AccountOperationErrorKind.Validation => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [error.Field ?? "request"] = [error.Message]
            }),
            AccountOperationErrorKind.Conflict => TypedResults.Problem(
                title: error.Title,
                detail: error.Message,
                statusCode: StatusCodes.Status409Conflict),
            _ => throw new InvalidOperationException($"Unsupported account operation result: {error.Kind}.")
        };
    }
}
