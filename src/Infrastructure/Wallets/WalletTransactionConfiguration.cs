using Domain.Shared;
using Domain.Wallets.Entities;
using Domain.Wallets.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Wallets;

internal sealed class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("wallet_transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.WalletId)
            .IsRequired();

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        ValueConverter<CurrencyCode, string> currencyCodeConverter = new(
            cc => cc.Value,
            s => new CurrencyCode(s));

        builder.Property(t => t.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasConversion(currencyCodeConverter);

        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(28, 8);

        builder.Property(t => t.OccurredAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasConversion(d => DateTime.SpecifyKind(d, DateTimeKind.Utc), v => v);
        
        
        builder.HasIndex(t => new { t.WalletId, t.OccurredAt })
            .HasDatabaseName("ix_wallet_transactions_wallet_id_occurred_at");
    }
}

