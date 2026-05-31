using Domain.ExchangeRates;
using Domain.ExchangeRates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ExchangeRates;

internal sealed class ExchangeRateTableConfiguration : IEntityTypeConfiguration<ExchangeRateTable>
{
    public void Configure(EntityTypeBuilder<ExchangeRateTable> builder)
    {
        builder.ToTable("exchange_rate_tables");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TableNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.PublicationDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(t => t.ImportedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasConversion(d => DateTime.SpecifyKind(d, DateTimeKind.Utc), v => v);

        // Idempotency: prevent duplicate imports for the same date or table number.
        builder.HasIndex(t => t.PublicationDate)
            .IsUnique()
            .HasDatabaseName("ix_exchange_rate_tables_publication_date");

        builder.HasIndex(t => t.TableNumber)
            .IsUnique()
            .HasDatabaseName("ix_exchange_rate_tables_table_number");

        builder.HasMany(t => t.Rates)
            .WithOne()
            .HasForeignKey(r => r.ExchangeRateTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Rates)
            .HasField("_rates")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}



