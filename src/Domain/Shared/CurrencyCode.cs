namespace Domain.Shared;


public sealed record CurrencyCode
{
    public static readonly CurrencyCode PLN = new("PLN");

    public string Value { get; }

    public CurrencyCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Currency code cannot be empty.", nameof(value));
        }

        if (normalized.Length != 3 || !normalized.All(char.IsLetter))
        {
            throw new ArgumentException(
                "Currency code must be exactly 3 letters", nameof(value));
        }

        Value = normalized;
    }


    private CurrencyCode() => Value = string.Empty;

    public override string ToString() => Value;
}

