using Domain.Shared;
using SharedKernel;

namespace Domain.Wallets;

public static class WalletErrors
{
    public static Error NotFound(Guid walletId) => Error.NotFound(
        "Wallets.NotFound",
        $"The wallet with Id = '{walletId}' was not found.");

    public static readonly Error Unauthorized = Error.Failure(
        "Wallets.Unauthorized",
        "You are not authorized to access this wallet.");

    public static readonly Error InvalidAmount = Error.Failure(
        "Wallets.InvalidAmount",
        "Amount must be greater than zero.");

    public static readonly Error InsufficientFunds = Error.Failure(
        "Wallets.InsufficientFunds",
        "Insufficient funds in the source currency balance.");

    public static readonly Error SameCurrency = Error.Failure(
        "Wallets.SameCurrency",
        "Source and target currencies must be different.");

    public static Error BalanceNotFound(CurrencyCode currency) => Error.NotFound(
        "Wallets.BalanceNotFound",
        $"No balance found for currency '{currency.Value}' in this wallet.");

    public static readonly Error NameEmpty = Error.Failure(
        "Wallets.NameEmpty",
        "Wallet name cannot be empty.");

    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "Wallets.ConcurrencyConflict",
        "The wallet was modified by another request. Please retry the operation.");
}


