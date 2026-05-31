using Domain.Shared;
using Domain.Wallets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Wallets;

internal sealed class WalletBalanceConfiguration : IEntityTypeConfiguration<WalletBalance>
{
    public void Configure(EntityTypeBuilder<WalletBalance> builder)
    {
        builder.ToTable("wallet_balances");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.WalletId)
            .IsRequired();

        ValueConverter<CurrencyCode, string> currencyCodeConverter = new(
            cc => cc.Value,
            s => new CurrencyCode(s));

        builder.Property(b => b.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasConversion(currencyCodeConverter);

        builder.Property(b => b.Amount)
            .IsRequired()
            .HasPrecision(28, 8);


        builder.HasIndex(b => new { b.WalletId, b.CurrencyCode })
            .IsUnique()
            .HasDatabaseName("ix_wallet_balances_wallet_id_currency_code");
    }
}


