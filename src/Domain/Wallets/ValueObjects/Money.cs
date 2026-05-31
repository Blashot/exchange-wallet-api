using Domain.Shared;

namespace Domain.Wallets.ValueObjects;


public sealed record Money
{
    public decimal Amount { get; }

    public CurrencyCode Currency { get; }

    public Money(decimal amount, CurrencyCode currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Money amount cannot be negative.");
        }

        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public override string ToString() => $"{Amount} {Currency}";
}
