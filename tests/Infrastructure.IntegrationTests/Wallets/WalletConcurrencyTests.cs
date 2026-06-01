using Domain.Shared;
using Domain.Users;
using Domain.Wallets;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests.Wallets;

[Collection("PostgresCollection")]
public sealed class WalletConcurrencyTests(PostgresContainerFixture fixture)
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private async Task<(Guid UserId, Guid WalletId)> SeedWalletAsync(decimal initialBalance)
    {
        await using ApplicationDbContext ctx = fixture.CreateDbContext();

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@test.com",
            FirstName = "Concurrent",
            LastName = "User",
            PasswordHash = "hash"
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        Wallet wallet = Wallet.Create(user.Id, "Concurrent Wallet", Now).Value;
        wallet.Deposit(CurrencyCode.PLN, initialBalance, Now);

        ctx.Wallets.Add(wallet);
        await ctx.SaveChangesAsync();

        return (user.Id, wallet.Id);
    }

    [Fact]
    public async Task SimultaneousWithdrawals_OnSameWallet_ShouldThrowConcurrencyException()
    {
        // Arrange: wallet with exactly 100 PLN — only one withdrawal should win.
        (_, Guid walletId) = await SeedWalletAsync(100m);

        await using ApplicationDbContext ctx1 = fixture.CreateDbContext();
        await using ApplicationDbContext ctx2 = fixture.CreateDbContext();

        // Simulate two concurrent requests reading the same wallet version.
        Wallet wallet1 = await ctx1.Wallets
            .Include(w => w.Balances)
            .SingleAsync(w => w.Id == walletId);

        Wallet wallet2 = await ctx2.Wallets
            .Include(w => w.Balances)
            .SingleAsync(w => w.Id == walletId);

        // Both withdrawals succeed against their local snapshots.
        wallet1.Withdraw(CurrencyCode.PLN, 100m, Now).IsSuccess.ShouldBeTrue();
        wallet2.Withdraw(CurrencyCode.PLN, 100m, Now).IsSuccess.ShouldBeTrue();

        // First save wins and bumps the wallet version.
        await ctx1.SaveChangesAsync();

        // Second save must fail because the wallet version has changed.
        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            async () => await ctx2.SaveChangesAsync());
    }

    [Fact]
    public async Task SequentialWithdrawals_ShouldBothSucceedWhenSufficientFunds()
    {
        // Sequential updates should succeed because each context reads the latest version.
        (_, Guid walletId) = await SeedWalletAsync(200m);

        await using ApplicationDbContext ctx1 = fixture.CreateDbContext();

        Wallet wallet1 = await ctx1.Wallets
            .Include(w => w.Balances)
            .SingleAsync(w => w.Id == walletId);

        wallet1.Withdraw(CurrencyCode.PLN, 100m, Now);
        await ctx1.SaveChangesAsync();

        await using ApplicationDbContext ctx2 = fixture.CreateDbContext();

        Wallet wallet2 = await ctx2.Wallets
            .Include(w => w.Balances)
            .SingleAsync(w => w.Id == walletId);

        wallet2.Withdraw(CurrencyCode.PLN, 100m, Now);
        await ctx2.SaveChangesAsync();

        await using ApplicationDbContext readCtx = fixture.CreateDbContext();

        Wallet loaded = await readCtx.Wallets
            .Include(w => w.Balances)
            .SingleAsync(w => w.Id == walletId);

        loaded.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(0m);
    }

    [Fact]
    public async Task ChildEntityChanges_ShouldBumpWalletVersionOnSave()
    {
        // Capture the version persisted during wallet creation.
        (_, Guid walletId) = await SeedWalletAsync(100m);

        Guid versionAfterSeed;

        await using (ApplicationDbContext snapCtx = fixture.CreateDbContext())
        {
            Wallet wallet = await snapCtx.Wallets.SingleAsync(w => w.Id == walletId);
            versionAfterSeed = wallet.Version;
        }

        versionAfterSeed.ShouldNotBe(Guid.Empty);

        await using (ApplicationDbContext writeCtx = fixture.CreateDbContext())
        {
            Wallet wallet = await writeCtx.Wallets
                .Include(w => w.Balances)
                .SingleAsync(w => w.Id == walletId);

            // Deposit changes child entities, so the wallet version should be bumped.
            wallet.Deposit(CurrencyCode.PLN, 50m, Now);

            await writeCtx.SaveChangesAsync();
        }

        await using ApplicationDbContext verifyCtx = fixture.CreateDbContext();

        Wallet reloaded = await verifyCtx.Wallets.SingleAsync(w => w.Id == walletId);

        // Verify that the concurrency token was updated.
        reloaded.Version.ShouldNotBe(versionAfterSeed);
        reloaded.Version.ShouldNotBe(Guid.Empty);
    }
}
