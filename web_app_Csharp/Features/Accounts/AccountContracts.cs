namespace web_app_Csharp.Features.Accounts;

public sealed record CreateAccountRequest(
    string OwnerId,
    string Name,
    AccountType Type);

public sealed record AccountTransactionRequest(decimal Amount);

public sealed record AccountResponse(
    int Id,
    string OwnerId,
    string Name,
    AccountType Type,
    decimal Balance)
{
    public static AccountResponse FromEntity(BankAccount account) =>
        new(account.Id, account.OwnerId, account.Name, account.Type, account.Balance);
}
