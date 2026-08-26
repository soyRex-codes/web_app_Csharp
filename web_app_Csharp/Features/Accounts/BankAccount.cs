namespace web_app_Csharp.Features.Accounts;

public sealed class BankAccount
{
    public const int OwnerIdMaxLength = 450;
    public const int NameMaxLength = 100;

    // EF Core uses this constructor when materializing an account from the database.
    private BankAccount()
    {
    }

    public BankAccount(string ownerId, string name, AccountType type)
    {
        var normalizedOwnerId = ownerId?.Trim();
        if (string.IsNullOrEmpty(normalizedOwnerId))
        {
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        }

        if (normalizedOwnerId.Length > OwnerIdMaxLength)
        {
            throw new ArgumentException($"Owner ID cannot exceed {OwnerIdMaxLength} characters.", nameof(ownerId));
        }

        var normalizedName = name?.Trim();
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new ArgumentException("Account name is required.", nameof(name));
        }

        if (normalizedName.Length > NameMaxLength)
        {
            throw new ArgumentException($"Account name cannot exceed {NameMaxLength} characters.", nameof(name));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Account type is not supported.");
        }

        OwnerId = normalizedOwnerId;
        Name = normalizedName;
        Type = type;
    }

    public int Id { get; private set; }

    public string OwnerId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public AccountType Type { get; private set; }

    // Balance changes only through operations that enforce the account's business rules.
    public decimal Balance { get; private set; }

    public void Deposit(decimal amount)
    {
        EnsurePositiveAmount(amount);
        Balance += amount;
    }

    public void Withdraw(decimal amount)
    {
        EnsurePositiveAmount(amount);

        if (amount > Balance)
        {
            throw new InvalidOperationException("The account has insufficient funds.");
        }

        Balance -= amount;
    }

    private static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot have more than two decimal places.");
        }
    }
}
