using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using web_app_Csharp.Features.Accounts;

namespace web_app_Csharp.Data.Configurations;

public sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.OwnerId)
            .HasMaxLength(BankAccount.OwnerIdMaxLength)
            .IsRequired();

        builder.Property(account => account.Name)
            .HasMaxLength(BankAccount.NameMaxLength)
            .IsRequired();

        builder.Property(account => account.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(account => account.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(account => account.OwnerId);
    }
}
