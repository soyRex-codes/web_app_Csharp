using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using web_app_Csharp.Features.Accounts;
using web_app_Csharp.Features.Identity;

namespace web_app_Csharp.Data;

public sealed class BankContext(DbContextOptions<BankContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options), IDataProtectionKeyContext
{
    public DbSet<BankAccount> Accounts => Set<BankAccount>();
    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankContext).Assembly);
    }
}
