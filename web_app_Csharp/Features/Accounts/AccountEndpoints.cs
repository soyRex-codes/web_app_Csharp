using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using web_app_Csharp.Data;

namespace web_app_Csharp.Features.Accounts;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/accounts")
            .WithTags("Accounts");

        group.MapGet("", GetAccounts);
        group.MapGet("/{id:int}", GetAccount);
        group.MapGet("/{id:int}/transactions", GetTransactions);
        group.MapPost("", CreateAccount);
        group.MapPost("/{id:int}/deposits", Deposit);
        group.MapPost("/{id:int}/withdrawals", Withdraw);

        return endpoints;
    }

    private static async Task<Ok<List<AccountResponse>>> GetAccounts(
        BankContext context,
        CancellationToken cancellationToken)
    {
        var accounts = await context.Accounts
            .AsNoTracking()
            .OrderBy(account => account.Id)
            .Select(account => new AccountResponse(
                account.Id,
                account.OwnerId,
                account.Name,
                account.Type,
                account.Balance))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(accounts);
    }

    private static async Task<Results<Ok<AccountResponse>, NotFound>> GetAccount(
        int id,
        BankContext context,
        CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return account is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(AccountResponse.FromEntity(account));
    }

    private static async Task<Results<Ok<List<AccountTransactionResponse>>, NotFound>> GetTransactions(
        int id,
        BankContext context,
        CancellationToken cancellationToken)
    {
        var accountExists = await context.Accounts.AnyAsync(account => account.Id == id, cancellationToken);
        if (!accountExists)
        {
            return TypedResults.NotFound();
        }

        var transactions = await context.AccountTransactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == id)
            .OrderByDescending(transaction => transaction.OccurredAtUtc)
            .ThenByDescending(transaction => transaction.Id)
            .Select(transaction => new AccountTransactionResponse(
                transaction.Id,
                transaction.Type,
                transaction.Amount,
                transaction.BalanceAfter,
                transaction.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(transactions);
    }

    private static async Task<Results<Created<AccountResponse>, ValidationProblem>> CreateAccount(
        CreateAccountRequest request,
        BankContext context,
        ILogger<BankAccount> logger,
        CancellationToken cancellationToken)
    {
        BankAccount account;

        try
        {
            account = new BankAccount(request.OwnerId, request.Name, request.Type);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception);
        }

        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created account {AccountId} for owner {OwnerId}",
            account.Id,
            account.OwnerId);

        var response = AccountResponse.FromEntity(account);
        return TypedResults.Created($"/api/v1/accounts/{account.Id}", response);
    }

    private static async Task<Results<Ok<AccountResponse>, NotFound, ValidationProblem>> Deposit(
        int id,
        AccountTransactionRequest request,
        BankContext context,
        ILogger<BankAccount> logger,
        CancellationToken cancellationToken)
    {
        var account = await context.Accounts.FindAsync([id], cancellationToken);
        if (account is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            account.Deposit(request.Amount);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return ValidationProblem(exception);
        }

        context.AccountTransactions.Add(new AccountTransaction(
            account.Id,
            AccountTransactionType.Deposit,
            request.Amount,
            account.Balance,
            DateTime.UtcNow));

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deposited {Amount} into account {AccountId}",
            request.Amount,
            account.Id);

        return TypedResults.Ok(AccountResponse.FromEntity(account));
    }

    private static async Task<Results<Ok<AccountResponse>, NotFound, ValidationProblem, ProblemHttpResult>> Withdraw(
        int id,
        AccountTransactionRequest request,
        BankContext context,
        ILogger<BankAccount> logger,
        CancellationToken cancellationToken)
    {
        var account = await context.Accounts.FindAsync([id], cancellationToken);
        if (account is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            account.Withdraw(request.Amount);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return ValidationProblem(exception);
        }
        catch (InvalidOperationException)
        {
            return TypedResults.Problem(
                title: "Withdrawal rejected",
                detail: "The account has insufficient funds.",
                statusCode: StatusCodes.Status409Conflict);
        }

        context.AccountTransactions.Add(new AccountTransaction(
            account.Id,
            AccountTransactionType.Withdrawal,
            request.Amount,
            account.Balance,
            DateTime.UtcNow));

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Withdrew {Amount} from account {AccountId}",
            request.Amount,
            account.Id);

        return TypedResults.Ok(AccountResponse.FromEntity(account));
    }

    private static ValidationProblem ValidationProblem(ArgumentException exception)
    {
        var key = exception.ParamName ?? "request";

        return TypedResults.ValidationProblem(
            new Dictionary<string, string[]>
            {
                [key] = [exception.Message]
            });
    }
}
