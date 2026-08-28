using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace web_app_Csharp.Data;

public sealed class BankContextFactory : IDesignTimeDbContextFactory<BankContext>
{
    public BankContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<BankContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("BankDatabase")
            ?? "Server=localhost;Database=BankingApp;Integrated Security=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<BankContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BankContext(options);
    }
}
