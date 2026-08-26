using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using web_app_Csharp.Features.Accounts;
using web_app_Csharp.Features.Identity;

namespace web_app_Csharp.Data;

public sealed class BankContext(DbContextOptions<BankContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BankAccount> Accounts => Set<BankAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankContext).Assembly);
    }
}
