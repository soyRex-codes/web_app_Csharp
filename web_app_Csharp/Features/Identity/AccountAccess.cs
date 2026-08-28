using System.Security.Claims;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Features.Identity;

public static class AccountAccess
{
    public static string? GetUserId(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static bool CanAccess(BankAccount account, ClaimsPrincipal user) =>
        user.IsInRole(ApplicationRoles.Admin) ||
        string.Equals(account.OwnerId, GetUserId(user), StringComparison.Ordinal);
}
