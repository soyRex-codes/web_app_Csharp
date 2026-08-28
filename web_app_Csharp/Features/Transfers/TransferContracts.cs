using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Features.Transfers;

public sealed record TransferRequest(
    int FromAccountId,
    int ToAccountId,
    decimal Amount);

public sealed record TransferResponse(
    AccountResponse FromAccount,
    AccountResponse ToAccount);
