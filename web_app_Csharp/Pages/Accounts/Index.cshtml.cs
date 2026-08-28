using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Pages.Accounts;

[Authorize]
public sealed class IndexModel(AccountOperationsService operations) : PageModel
{
    public IReadOnlyList<AccountResponse> Accounts { get; private set; } = [];
    public decimal TotalBalance => Accounts.Sum(account => account.Balance);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadPageAsync(cancellationToken);

    public async Task<IActionResult> OnPostCreateAsync(
        string? name,
        AccountType type,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await LoadPageAsync(cancellationToken);
        }

        var result = await operations.CreateAsync(new CreateAccountRequest(name ?? string.Empty, type), User, cancellationToken);
        if (!result.IsSuccess)
        {
            AddOperationError(result.Error!);
            return await LoadPageAsync(cancellationToken);
        }

        TempData["Success"] = "Your account was opened.";
        return RedirectToPage("/Accounts/Details", new { id = result.Value!.Id });
    }

    private async Task<IActionResult> LoadPageAsync(CancellationToken cancellationToken)
    {
        var result = await operations.GetAccountsAsync(User, cancellationToken);
        if (!result.IsSuccess)
        {
            return Forbid();
        }

        Accounts = result.Value!;
        return Page();
    }

    private void AddOperationError(AccountOperationError error) =>
        ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
}
