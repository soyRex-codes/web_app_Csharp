using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Tests;

public sealed class BankAccountTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesAccountWithZeroBalance()
    {
        var account = CreateAccount();

        Assert.Equal("user-123", account.OwnerId);
        Assert.Equal("Everyday Checking", account.Name);
        Assert.Equal(AccountType.Checking, account.Type);
        Assert.Equal(0m, account.Balance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankOwnerId_ThrowsArgumentException(string ownerId)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new BankAccount(ownerId, "Everyday Checking", AccountType.Checking));

        Assert.Equal("ownerId", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankName_ThrowsArgumentException(string name)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new BankAccount("user-123", name, AccountType.Checking));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_InputWithWhitespace_TrimsStoredValues()
    {
        var account = new BankAccount("  user-123  ", "  Primary  ", AccountType.Checking);

        Assert.Equal("user-123", account.OwnerId);
        Assert.Equal("Primary", account.Name);
    }

    [Fact]
    public void Constructor_UnsupportedType_ThrowsArgumentOutOfRangeException()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BankAccount("user-123", "Primary", (AccountType)999));

        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void Deposit_PositiveAmount_IncreasesBalance()
    {
        var account = CreateAccount();

        account.Deposit(500m);

        Assert.Equal(500m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Deposit_NonPositiveAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var account = CreateAccount();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount));

        Assert.Equal("amount", exception.ParamName);
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Deposit_AmountWithFractionalCents_ThrowsArgumentOutOfRangeException()
    {
        var account = CreateAccount();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(10.001m));

        Assert.Equal("amount", exception.ParamName);
        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void Withdraw_AvailableAmount_DecreasesBalance()
    {
        var account = CreateAccount();
        account.Deposit(1_000m);

        account.Withdraw(300m);

        Assert.Equal(700m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Withdraw_NonPositiveAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        var account = CreateAccount();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => account.Withdraw(amount));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Withdraw_AmountGreaterThanBalance_ThrowsAndDoesNotChangeBalance()
    {
        var account = CreateAccount();
        account.Deposit(100m);

        var exception = Assert.Throws<InvalidOperationException>(() => account.Withdraw(500m));

        Assert.Contains("insufficient funds", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100m, account.Balance);
    }

    private static BankAccount CreateAccount() =>
        new("user-123", "Everyday Checking", AccountType.Checking);
}
