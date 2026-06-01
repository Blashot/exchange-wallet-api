using Application.Abstractions.Data;
using Domain.ExchangeRates;
using Domain.ExchangeRates.Entities;
using Domain.Users;
using Domain.Wallets;
using Domain.Wallets.Entities;
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }
    
    // ExchangeRates
    public DbSet<ExchangeRateTable> ExchangeRateTables { get; set; }
    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    
    // Wallets
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletBalance> WalletBalances { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.HasDefaultSchema(Schemas.Default);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        
        UpdateWalletConcurrencyTokens();
        
        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync();

        return result;
    }
    
    private void UpdateWalletConcurrencyTokens()
    {
        HashSet<Guid> walletIdsWithChanges = [];
        List<EntityEntry> newChildEntitiesMarkedAsModified = [];

        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Added or EntityState.Deleted))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case WalletBalance balance:
                    walletIdsWithChanges.Add(balance.WalletId);

                    if (entry.State == EntityState.Modified)
                    {
                        newChildEntitiesMarkedAsModified.Add(entry);
                    }

                    break;

                case WalletTransaction transaction:
                    walletIdsWithChanges.Add(transaction.WalletId);

                    if (entry.State == EntityState.Modified)
                    {
                        newChildEntitiesMarkedAsModified.Add(entry);
                    }

                    break;
            }
        }

        // New child entities discovered during DetectChanges can be incorrectly
        // tracked as Modified. Convert them to Added so EF generates INSERTs.
        foreach (EntityEntry entry in newChildEntitiesMarkedAsModified)
        {
            bool hasActualChanges = entry.Properties
                .Any(property => !Equals(property.OriginalValue, property.CurrentValue));

            if (!hasActualChanges)
            {
                entry.State = EntityState.Added;
            }
        }

        foreach (EntityEntry<Wallet> entry in ChangeTracker.Entries<Wallet>())
        {
            if (entry.State is EntityState.Added or EntityState.Deleted)
            {
                continue;
            }

            // Bump the aggregate version when the wallet or its child collections change.
            if (entry.State == EntityState.Modified || walletIdsWithChanges.Contains(entry.Entity.Id))
            {
                entry.Property(wallet => wallet.Version).CurrentValue = Guid.NewGuid();
            }
        }
    }

    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }
}
