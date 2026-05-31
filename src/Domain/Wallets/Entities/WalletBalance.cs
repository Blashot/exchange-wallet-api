using Domain.Shared;
using SharedKernel;

namespace Domain.Wallets.Entities;

/// <summary>
/// Tracks the current balance of a single currency within a <see cref="Wallet"/>.
/// </summary>
public sealed class WalletBalance : Entity
{
    public Guid Id { get; private set; }

    public Guid WalletId { get; private set; }

    public CurrencyCode CurrencyCode { get; private set; }

    public decimal Amount { get; private set; }
    
    
    private WalletBalance()
    {
        CurrencyCode = CurrencyCode.PLN;
    }

    internal static WalletBalance Create(Guid walletId, CurrencyCode currencyCode)
    {
        return new WalletBalance
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            CurrencyCode = currencyCode,
            Amount = 0m
        };
    }
}
