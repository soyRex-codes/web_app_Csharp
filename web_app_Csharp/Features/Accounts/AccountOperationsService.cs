using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using web_app_Csharp.Data;
using web_app_Csharp.Features.Identity;
using web_app_Csharp.Features.Transfers;

namespace web_app_Csharp.Features.Accounts;

// Keeps the account workflows used by both HTTP APIs and Razor Pages in one place.
public sealed class AccountOperationsService(
    BankContext context,
    ILogger<AccountOperationsService> logger)
{
    public async Task<AccountOperationResult<IReadOnlyList<AccountResponse>>> GetAccountsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        IQueryable<BankAccount> query = context.Accounts.AsNoTracking();
        if (!user.IsInRole(ApplicationRoles.Admin))
        {
            var userId = AccountAccess.GetUserId(user);
            if (userId is null)
            {
                return Forbidden<IReadOnlyList<AccountResponse>>();
            }

            query = query.Where(account => account.OwnerId == userId);
        }

        var accounts = await query
            .OrderBy(account => account.Id)
            .Select(account => new AccountResponse(
                account.Id,
                account.OwnerId,
                account.Name,
                account.Type,
                account.Balance))
            .ToListAsync(cancellationToken);

        return AccountOperationResult<IReadOnlyList<AccountResponse>>.Success(accounts);
    }

    public async Task<AccountOperationResult<AccountResponse>> GetAccountAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (account is null)
        {
            return NotFound<AccountResponse>();
        }

        return AccountAccess.CanAccess(account, user)
            ? AccountOperationResult<AccountResponse>.Success(AccountResponse.FromEntity(account))
            : Forbidden<AccountResponse>();
    }

    public async Task<AccountOperationResult<IReadOnlyList<AccountTransactionResponse>>> GetTransactionsAsync(
        int accountId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var account = await context.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken);
        if (account is null)
        {
            return NotFound<IReadOnlyList<AccountTransactionResponse>>();
        }

        if (!AccountAccess.CanAccess(account, user))
        {
            return Forbidden<IReadOnlyList<AccountTransactionResponse>>();
        }

        var transactions = await context.AccountTransactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .OrderByDescending(transaction => transaction.OccurredAtUtc)
            .ThenByDescending(transaction => transaction.Id)
            .Select(transaction => new AccountTransactionResponse(
                transaction.Id,
                transaction.Type,
                transaction.Amount,
                transaction.BalanceAfter,
                transaction.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return AccountOperationResult<IReadOnlyList<AccountTransactionResponse>>.Success(transactions);
    }

    public async Task<AccountOperationResult<AccountResponse>> CreateAsync(
        CreateAccountRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var ownerId = AccountAccess.GetUserId(user);
        if (ownerId is null)
        {
            return Forbidden<AccountResponse>();
        }

        BankAccount account;
        try
        {
            account = new BankAccount(ownerId, request.Name, request.Type);
        }
        catch (ArgumentException exception)
        {
            return Validation<AccountResponse>(exception);
        }

        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created account {AccountId} for owner {OwnerId}", account.Id, account.OwnerId);

        return AccountOperationResult<AccountResponse>.Success(AccountResponse.FromEntity(account));
    }

    public async Task<AccountOperationResult<AccountResponse>> DepositAsync(
        int id,
        AccountTransactionRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var accountResult = await FindAccessibleAccountAsync(id, user, cancellationToken);
        if (!accountResult.IsSuccess)
        {
            return Propagate<AccountResponse>(accountResult);
        }

        var account = accountResult.Value!;
        try
        {
            account.Deposit(request.Amount);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Validation<AccountResponse>(exception);
        }

        context.AccountTransactions.Add(new AccountTransaction(
            account.Id,
            AccountTransactionType.Deposit,
            request.Amount,
            account.Balance,
            DateTime.UtcNow));
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deposited {Amount} into account {AccountId}", request.Amount, account.Id);

        return AccountOperationResult<AccountResponse>.Success(AccountResponse.FromEntity(account));
    }

    public async Task<AccountOperationResult<AccountResponse>> WithdrawAsync(
        int id,
        AccountTransactionRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var accountResult = await FindAccessibleAccountAsync(id, user, cancellationToken);
        if (!accountResult.IsSuccess)
        {
            return Propagate<AccountResponse>(accountResult);
        }

        var account = accountResult.Value!;
        try
        {
            account.Withdraw(request.Amount);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return Validation<AccountResponse>(exception);
        }
        catch (InvalidOperationException)
        {
            return Conflict<AccountResponse>("Withdrawal rejected", "The account has insufficient funds.");
        }

        context.AccountTransactions.Add(new AccountTransaction(
            account.Id,
            AccountTransactionType.Withdrawal,
            request.Amount,
            account.Balance,
            DateTime.UtcNow));
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Withdrew {Amount} from account {AccountId}", request.Amount, account.Id);

        return AccountOperationResult<AccountResponse>.Success(AccountResponse.FromEntity(account));
    }

    public async Task<AccountOperationResult<TransferResponse>> TransferAsync(
        TransferRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateTransfer(request);
        if (validationError is not null)
        {
            return AccountOperationResult<TransferResponse>.Failure(validationError);
        }

        // SQL Server retry strategies require the explicit transaction to be retried as one unit.
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async cancellationToken =>
        {
            await using var databaseTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var accounts = await context.Accounts
                .Where(account => account.Id == request.FromAccountId || account.Id == request.ToAccountId)
                .ToDictionaryAsync(account => account.Id, cancellationToken);

            if (!accounts.TryGetValue(request.FromAccountId, out var fromAccount) ||
                !accounts.TryGetValue(request.ToAccountId, out var toAccount))
            {
                return NotFound<TransferResponse>();
            }

            if (!AccountAccess.CanAccess(fromAccount, user) || !AccountAccess.CanAccess(toAccount, user))
            {
                return Forbidden<TransferResponse>();
            }

            try
            {
                fromAccount.Withdraw(request.Amount);
            }
            catch (InvalidOperationException)
            {
                return Conflict<TransferResponse>("Transfer rejected", "The source account has insufficient funds.");
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

            logger.LogInformation(
                "Transferred {Amount} from account {FromAccountId} to account {ToAccountId}",
                request.Amount,
                fromAccount.Id,
                toAccount.Id);

            return AccountOperationResult<TransferResponse>.Success(new TransferResponse(
                AccountResponse.FromEntity(fromAccount),
                AccountResponse.FromEntity(toAccount)));
        }, cancellationToken);
    }

    private async Task<AccountOperationResult<BankAccount>> FindAccessibleAccountAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var account = await context.Accounts.FindAsync([id], cancellationToken);
        if (account is null)
        {
            return NotFound<BankAccount>();
        }

        return AccountAccess.CanAccess(account, user)
            ? AccountOperationResult<BankAccount>.Success(account)
            : Forbidden<BankAccount>();
    }

    private static AccountOperationError? ValidateTransfer(TransferRequest request)
    {
        if (request.FromAccountId <= 0)
        {
            return new(AccountOperationErrorKind.Validation, "Source account ID must be positive.", nameof(request.FromAccountId));
        }

        if (request.ToAccountId <= 0)
        {
            return new(AccountOperationErrorKind.Validation, "Destination account ID must be positive.", nameof(request.ToAccountId));
        }

        if (request.FromAccountId == request.ToAccountId)
        {
            return new(AccountOperationErrorKind.Validation, "Source and destination accounts must be different.", nameof(request.ToAccountId));
        }

        if (request.Amount <= 0 || decimal.Round(request.Amount, 2) != request.Amount)
        {
            return new(AccountOperationErrorKind.Validation, "Amount must be positive and use no more than two decimal places.", nameof(request.Amount));
        }

        return null;
    }

    private static AccountOperationResult<T> NotFound<T>() =>
        AccountOperationResult<T>.Failure(new(AccountOperationErrorKind.NotFound, "The account was not found."));

    private static AccountOperationResult<T> Forbidden<T>() =>
        AccountOperationResult<T>.Failure(new(AccountOperationErrorKind.Forbidden, "You do not have access to this account."));

    private static AccountOperationResult<T> Conflict<T>(string title, string message) =>
        AccountOperationResult<T>.Failure(new(AccountOperationErrorKind.Conflict, message, Title: title));

    private static AccountOperationResult<T> Validation<T>(ArgumentException exception) =>
        AccountOperationResult<T>.Failure(new(
            AccountOperationErrorKind.Validation,
            exception.Message,
            exception.ParamName ?? "request"));

    private static AccountOperationResult<T> Propagate<T>(AccountOperationResult<BankAccount> result) =>
        AccountOperationResult<T>.Failure(result.Error!);
}
