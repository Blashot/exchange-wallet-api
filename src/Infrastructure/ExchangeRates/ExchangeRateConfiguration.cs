using Domain.ExchangeRates.Entities;
using Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.ExchangeRates;

internal sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ExchangeRateTableId)
            .IsRequired();

        ValueConverter<CurrencyCode, string> currencyCodeConverter = new(
            cc => cc.Value,
            s => new CurrencyCode(s));

        builder.Property(r => r.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .HasConversion(currencyCodeConverter);

        builder.Property(r => r.CurrencyName)
            .IsRequired()
            .HasMaxLength(100);


        builder.Property(r => r.MidRate)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.HasIndex(r => new { r.ExchangeRateTableId, r.CurrencyCode })
            .IsUnique()
            .HasDatabaseName("ix_exchange_rates_table_id_currency_code");
    }
}

