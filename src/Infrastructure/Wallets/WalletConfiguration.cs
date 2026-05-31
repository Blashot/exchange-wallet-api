using Domain.Users;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Wallets;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId)
            .IsRequired();

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone")
            .HasConversion(d => DateTime.SpecifyKind(d, DateTimeKind.Utc), v => v);


        builder.Property(w => w.Version)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("ix_wallets_user_id");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasMany(w => w.Balances)
            .WithOne()
            .HasForeignKey(b => b.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Balances)
            .HasField("_balances")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(w => w.Transactions)
            .WithOne()
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Transactions)
            .HasField("_transactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}


