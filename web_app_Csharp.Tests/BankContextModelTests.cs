using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web_app_Csharp.Data;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Tests;

public sealed class BankContextModelTests
{
    [Fact]
    public void Model_BankAccount_UsesExpectedSqlServerMapping()
    {
        var options = new DbContextOptionsBuilder<BankContext>()
            .UseSqlServer("Server=localhost;Database=ModelTest;Integrated Security=True")
            .Options;

        using var context = new BankContext(options);

        var account = context.Model.FindEntityType(typeof(BankAccount));
        Assert.NotNull(account);
        Assert.Equal("Accounts", account.GetTableName());

        var balance = account.FindProperty(nameof(BankAccount.Balance));
        Assert.NotNull(balance);
        Assert.Equal(18, balance.GetPrecision());
        Assert.Equal(2, balance.GetScale());

        Assert.Contains(
            account.GetIndexes(),
            index => index.Properties.Count == 1
                     && index.Properties[0].Name == nameof(BankAccount.OwnerId));
    }

    [Fact]
    public void Model_DataProtectionKeys_UsesExpectedSqlServerMapping()
    {
        var options = new DbContextOptionsBuilder<BankContext>()
            .UseSqlServer("Server=localhost;Database=ModelTest;Integrated Security=True")
            .Options;

        using var context = new BankContext(options);

        var keys = context.Model.FindEntityType(typeof(DataProtectionKey));
        Assert.NotNull(keys);
        Assert.Equal("DataProtectionKeys", keys.GetTableName());
    }
}
