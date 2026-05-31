using Domain.Shared;
using Domain.Wallets.Enums;
using SharedKernel;

namespace Domain.Wallets.Entities;

/// <summary>
/// Immutable ledger entry recording a single money movement inside a <see cref="Wallet"/>.
/// </summary>
public sealed class WalletTransaction : Entity
{
    public Guid Id { get; private set; }

    public Guid WalletId { get; private set; }

    public TransactionType Type { get; private set; }

    public CurrencyCode CurrencyCode { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime OccurredAt { get; private set; }

    private WalletTransaction()
    {
        CurrencyCode = CurrencyCode.PLN;
    }

    internal static WalletTransaction Create(
        Guid walletId,
        TransactionType type,
        CurrencyCode currencyCode,
        decimal amount,
        DateTime occurredAt)
    {
        return new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Type = type,
            CurrencyCode = currencyCode,
            Amount = amount,
            OccurredAt = occurredAt
        };
    }
}

