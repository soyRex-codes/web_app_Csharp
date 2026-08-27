using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Tests;

public sealed class AccountTransactionTests
{
    [Fact]
    public void Constructor_ValidInput_CapturesImmutableTransactionDetails()
    {
        var occurredAtUtc = new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);

        var transaction = new AccountTransaction(
            1,
            AccountTransactionType.Deposit,
            25m,
            125m,
            occurredAtUtc);

        Assert.Equal(1, transaction.AccountId);
        Assert.Equal(AccountTransactionType.Deposit, transaction.Type);
        Assert.Equal(25m, transaction.Amount);
        Assert.Equal(125m, transaction.BalanceAfter);
        Assert.Equal(occurredAtUtc, transaction.OccurredAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AccountTransaction(
            1,
            AccountTransactionType.Deposit,
            amount,
            0m,
            DateTime.UtcNow));
    }
}
