using Domain.ExchangeRates;
using Domain.ExchangeRates.Entities;
using Domain.Users;
using Domain.Wallets;
using Domain.Wallets.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    
    // ExchangeRates
    DbSet<ExchangeRateTable> ExchangeRateTables { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }
    
    // Wallets
    DbSet<Wallet> Wallets { get; }
    DbSet<WalletBalance> WalletBalances { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
