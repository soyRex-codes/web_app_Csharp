using Microsoft.EntityFrameworkCore;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Data;

public sealed class BankContext(DbContextOptions<BankContext> options) : DbContext(options)
{
    public DbSet<BankAccount> Accounts => Set<BankAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankContext).Assembly);
    }
}
