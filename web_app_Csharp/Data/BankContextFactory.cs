using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace web_app_Csharp.Data;

public sealed class BankContextFactory : IDesignTimeDbContextFactory<BankContext>
{
    public BankContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BankDatabase")
            ?? "Server=localhost;Database=BankingApp;Integrated Security=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<BankContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BankContext(options);
    }
}
