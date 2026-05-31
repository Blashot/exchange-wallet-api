using Domain.Wallets;

namespace Domain.UnitTests.Wallets;


public sealed class WalletVersionTests
{
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly Guid UserId = Guid.NewGuid();

    private static Wallet CreateWallet() =>
        Wallet.Create(UserId, "Test Wallet", Now).Value;

    [Fact]
    public void Create_ShouldInitialiseVersionToNonEmptyGuid()
    {
        Wallet wallet = CreateWallet();

        wallet.Version.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TwoWallets_ShouldHaveDifferentVersions()
    {
        Wallet a = CreateWallet();
        Wallet b = CreateWallet();

        a.Version.ShouldNotBe(b.Version);
    }
}
