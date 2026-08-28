using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using web_app_Csharp.Data;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Features.Transfers;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/transfers", Transfer)
            .WithTags("Transfers");

        return endpoints;
    }

    private static async Task<IResult> Transfer(
        TransferRequest request,
        BankContext context,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        // SQL Server retry strategies require the entire explicit transaction to be retried as one unit.
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IResult>(async cancellationToken =>
        {
            await using var databaseTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var accounts = await context.Accounts
                .Where(account => account.Id == request.FromAccountId || account.Id == request.ToAccountId)
                .ToDictionaryAsync(account => account.Id, cancellationToken);

            if (!accounts.TryGetValue(request.FromAccountId, out var fromAccount) ||
                !accounts.TryGetValue(request.ToAccountId, out var toAccount))
            {
                return TypedResults.NotFound();
            }

            try
            {
                fromAccount.Withdraw(request.Amount);
            }
            catch (InvalidOperationException)
            {
                return TypedResults.Problem(
                    title: "Transfer rejected",
                    detail: "The source account has insufficient funds.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            toAccount.Deposit(request.Amount);

            var occurredAtUtc = DateTime.UtcNow;
            context.AccountTransactions.AddRange(
                new AccountTransaction(
                    fromAccount.Id,
                    AccountTransactionType.TransferOut,
                    request.Amount,
                    fromAccount.Balance,
                    occurredAtUtc),
                new AccountTransaction(
                    toAccount.Id,
                    AccountTransactionType.TransferIn,
                    request.Amount,
                    toAccount.Balance,
                    occurredAtUtc));

            await context.SaveChangesAsync(cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);

            loggerFactory.CreateLogger("Transfers").LogInformation(
                "Transferred {Amount} from account {FromAccountId} to account {ToAccountId}",
                request.Amount,
                fromAccount.Id,
                toAccount.Id);

            return TypedResults.Ok(new TransferResponse(
                AccountResponse.FromEntity(fromAccount),
                AccountResponse.FromEntity(toAccount)));
        }, cancellationToken);
    }

    private static ValidationProblem? ValidateRequest(TransferRequest request)
    {
        if (request.FromAccountId <= 0)
        {
            return ValidationProblem(nameof(request.FromAccountId), "Source account ID must be positive.");
        }

        if (request.ToAccountId <= 0)
        {
            return ValidationProblem(nameof(request.ToAccountId), "Destination account ID must be positive.");
        }

        if (request.FromAccountId == request.ToAccountId)
        {
            return ValidationProblem(nameof(request.ToAccountId), "Source and destination accounts must be different.");
        }

        if (request.Amount <= 0 || decimal.Round(request.Amount, 2) != request.Amount)
        {
            return ValidationProblem(nameof(request.Amount), "Amount must be positive and use no more than two decimal places.");
        }

        return null;
    }

    private static ValidationProblem ValidationProblem(string key, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [key] = [message]
        });
}
