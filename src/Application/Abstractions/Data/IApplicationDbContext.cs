using Domain.ExchangeRates;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    
    // ExchangeRates
    DbSet<ExchangeRateTable> ExchangeRateTables { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
