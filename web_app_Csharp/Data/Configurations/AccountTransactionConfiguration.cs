using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Data.Configurations;

public sealed class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        builder.ToTable("AccountTransactions");
        builder.HasKey(transaction => transaction.Id);
        builder.Property(transaction => transaction.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(transaction => transaction.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(transaction => transaction.BalanceAfter).HasPrecision(18, 2).IsRequired();
        builder.Property(transaction => transaction.OccurredAtUtc).IsRequired();
        builder.HasIndex(transaction => new { transaction.AccountId, transaction.OccurredAtUtc });
        builder.HasOne<BankAccount>().WithMany().HasForeignKey(transaction => transaction.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
