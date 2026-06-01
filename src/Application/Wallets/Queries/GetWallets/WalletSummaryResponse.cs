namespace Application.Wallets.Queries.GetWallets;

public sealed class WalletSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BalanceSummaryResponse> Balances { get; set; } = [];
}

public sealed class BalanceSummaryResponse
{
    public string CurrencyCode { get; set; }
    public decimal Amount { get; set; }
}

