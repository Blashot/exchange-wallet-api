namespace Application.Wallets.Queries.GetWalletById;

public sealed class WalletResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BalanceResponse> Balances { get; set; } = [];
    public List<TransactionResponse> RecentTransactions { get; set; } = [];
}

public sealed class BalanceResponse
{
    public string CurrencyCode { get; set; }
    public decimal Amount { get; set; }
}

public sealed class TransactionResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string CurrencyCode { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }
}

