using Domain.Shared;
using SharedKernel;

namespace Domain.Wallets;

public static class WalletErrors
{
    public static readonly Error InvalidAmount = Error.Failure(
        "Wallets.InvalidAmount",
        "Amount must be greater than zero.");

    public static readonly Error InsufficientFunds = Error.Failure(
        "Wallets.InsufficientFunds",
        "Insufficient funds in the source currency balance.");

    public static readonly Error NameEmpty = Error.Failure(
        "Wallets.NameEmpty",
        "Wallet name cannot be empty.");
}


