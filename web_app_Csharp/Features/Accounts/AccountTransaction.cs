namespace web_app_Csharp.Features.Accounts;

public sealed class AccountTransaction
{
    private AccountTransaction()
    {
    }

    public AccountTransaction(
        int accountId,
        AccountTransactionType type,
        decimal amount,
        decimal balanceAfter,
        DateTime occurredAtUtc)
    {
        if (accountId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(accountId), accountId, "Account ID must be positive.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Transaction type is not supported.");
        }

        if (amount <= 0 || decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive and use no more than two decimal places.");
        }

        AccountId = accountId;
        Type = type;
        Amount = amount;
        BalanceAfter = balanceAfter;
        OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
    }

    public int Id { get; private set; }

    public int AccountId { get; private set; }

    public AccountTransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public decimal BalanceAfter { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }
}
