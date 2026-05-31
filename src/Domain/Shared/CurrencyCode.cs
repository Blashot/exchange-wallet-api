namespace Domain.Shared;


public sealed record CurrencyCode
{
    public static readonly CurrencyCode PLN = new("PLN");

    public string Value { get; }

    public CurrencyCode(string value)
    {
        Value = value.Trim().ToUpperInvariant();
    }

    private CurrencyCode() => Value = string.Empty;

    public override string ToString() => Value;
}

