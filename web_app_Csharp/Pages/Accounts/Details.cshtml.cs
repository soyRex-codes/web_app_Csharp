using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web_app_Csharp.Features.Accounts;
using web_app_Csharp.Features.Transfers;

namespace web_app_Csharp.Pages.Accounts;

[Authorize]
public sealed class DetailsModel(AccountOperationsService operations) : PageModel
{
    public AccountResponse? Account { get; private set; }
    public IReadOnlyList<AccountResponse> TransferDestinations { get; private set; } = [];
    public IReadOnlyList<AccountTransactionResponse> Transactions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken) =>
        await LoadPageAsync(id, cancellationToken);

    public Task<IActionResult> OnPostDepositAsync(int id, decimal amount, CancellationToken cancellationToken) =>
        CompleteMoneyOperationAsync(id, operations.DepositAsync(id, new AccountTransactionRequest(amount), User, cancellationToken), "Your deposit was completed.", cancellationToken);

    public Task<IActionResult> OnPostWithdrawAsync(int id, decimal amount, CancellationToken cancellationToken) =>
        CompleteMoneyOperationAsync(id, operations.WithdrawAsync(id, new AccountTransactionRequest(amount), User, cancellationToken), "Your withdrawal was completed.", cancellationToken);

    public async Task<IActionResult> OnPostTransferAsync(
        int id,
        int toAccountId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var result = await operations.TransferAsync(new TransferRequest(id, toAccountId, amount), User, cancellationToken);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Your transfer was completed.";
            return RedirectToPage(new { id });
        }

        AddOperationError(result.Error!);
        return await LoadPageAsync(id, cancellationToken);
    }

    private async Task<IActionResult> CompleteMoneyOperationAsync(
        int id,
        Task<AccountOperationResult<AccountResponse>> operation,
        string successMessage,
        CancellationToken cancellationToken)
    {
        var result = await operation;
        if (result.IsSuccess)
        {
            TempData["Success"] = successMessage;
            return RedirectToPage(new { id });
        }

        AddOperationError(result.Error!);
        return await LoadPageAsync(id, cancellationToken);
    }

    private async Task<IActionResult> LoadPageAsync(int id, CancellationToken cancellationToken)
    {
        var accountResult = await operations.GetAccountAsync(id, User, cancellationToken);
        if (!accountResult.IsSuccess)
        {
            return accountResult.Error!.Kind == AccountOperationErrorKind.NotFound ? NotFound() : Forbid();
        }

        Account = accountResult.Value!;

        var accountsResult = await operations.GetAccountsAsync(User, cancellationToken);
        if (!accountsResult.IsSuccess)
        {
            return Forbid();
        }

        TransferDestinations = accountsResult.Value!
            .Where(account => account.Id != id)
            .ToList();

        var transactionsResult = await operations.GetTransactionsAsync(id, User, cancellationToken);
        if (!transactionsResult.IsSuccess)
        {
            return Forbid();
        }

        Transactions = transactionsResult.Value!;
        return Page();
    }

    private void AddOperationError(AccountOperationError error) =>
        ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
}
