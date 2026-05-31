using Domain.Shared;
using SharedKernel;

namespace Domain.Wallets;

public static class WalletErrors
{
    public static readonly Error NameEmpty = Error.Failure(
        "Wallets.NameEmpty",
        "Wallet name cannot be empty.");
}


