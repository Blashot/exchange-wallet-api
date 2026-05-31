using Domain.Shared;
using Domain.Wallets.Entities;
using Domain.Wallets.Enums;
using SharedKernel;

namespace Domain.Wallets;

public sealed class Wallet : Entity
{
    private readonly List<WalletBalance> _balances = [];
    private readonly List<WalletTransaction> _transactions = [];

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid Version { get; private set; }

    public IReadOnlyList<WalletBalance> Balances => _balances.AsReadOnly();

    public IReadOnlyList<WalletTransaction> Transactions => _transactions.AsReadOnly();
    
    private Wallet()
    {
        Name = string.Empty;
    }
    
    public static Result<Wallet> Create(Guid userId, string name, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Wallet>(WalletErrors.NameEmpty);
        }

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            CreatedAt = createdAt,
            Version = Guid.NewGuid()
        };
        
        //TODO: Raise domain event after the wallet has been created.

        return Result.Success(wallet);
    }
    
}

