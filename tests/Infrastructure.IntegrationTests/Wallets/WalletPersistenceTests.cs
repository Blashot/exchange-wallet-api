using Domain.Shared;
using Domain.Users;
using Domain.Wallets;
using Domain.Wallets.Entities;
using Domain.Wallets.Enums;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests.Wallets;

[Collection("PostgresCollection")]
public sealed class WalletPersistenceTests(PostgresContainerFixture fixture)
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private async Task<(ApplicationDbContext Ctx, Guid UserId)> SeedUserAsync()
    {
        ApplicationDbContext ctx = fixture.CreateDbContext();

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@test.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };

        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        return (ctx, user.Id);
    }

    [Fact]
    public async Task Deposit_ShouldPersistBalanceAndTransaction()
    {
        (ApplicationDbContext ctx, Guid userId) = await SeedUserAsync();

        await using (ctx)
        {
            Wallet wallet = Wallet.Create(userId, "My Wallet", Now).Value;

            wallet.Deposit(CurrencyCode.PLN, 250m, Now);

            ctx.Wallets.Add(wallet);
            await ctx.SaveChangesAsync();
        }

        await using ApplicationDbContext readCtx = fixture.CreateDbContext();

        Wallet loaded = await readCtx.Wallets
            .Include(wallet => wallet.Balances)
            .Include(wallet => wallet.Transactions)
            .SingleAsync(wallet => wallet.UserId == userId);

        loaded.Balances.ShouldHaveSingleItem().Amount.ShouldBe(250m);

        WalletTransaction transaction = loaded.Transactions.ShouldHaveSingleItem();

        transaction.Type.ShouldBe(TransactionType.Deposit);
        transaction.CurrencyCode.ShouldBe(CurrencyCode.PLN);
        transaction.Amount.ShouldBe(250m);
    }

    [Fact]
    public async Task Withdraw_ShouldPersistReducedBalanceAndTransaction()
    {
        (ApplicationDbContext ctx, Guid userId) = await SeedUserAsync();
        Guid walletId = await SeedWalletWithDepositAsync(ctx, userId, CurrencyCode.PLN, 500m);

        await WithdrawAndSaveAsync(walletId, CurrencyCode.PLN, 200m);

        await using ApplicationDbContext readCtx = fixture.CreateDbContext();

        Wallet loaded = await readCtx.Wallets
            .Include(wallet => wallet.Balances)
            .Include(wallet => wallet.Transactions)
            .SingleAsync(wallet => wallet.Id == walletId);

        loaded.Balances
            .Single(balance => balance.CurrencyCode == CurrencyCode.PLN)
            .Amount
            .ShouldBe(300m);

        loaded.Transactions.Count.ShouldBe(2); // Deposit + withdrawal

        loaded.Transactions.ShouldContain(transaction =>
            transaction.Type == TransactionType.Withdrawal &&
            transaction.CurrencyCode == CurrencyCode.PLN &&
            transaction.Amount == 200m);
    }

    [Fact]
    public async Task Convert_ShouldPersistBothLegsAndUpdatedBalances()
    {
        CurrencyCode usd = new("USD");
        CurrencyCode eur = new("EUR");

        const decimal usdRate = 4.00m;
        const decimal eurRate = 4.50m;

        (ApplicationDbContext ctx, Guid userId) = await SeedUserAsync();
        Guid walletId = await SeedWalletWithDepositAsync(ctx, userId, usd, 100m);

        await ConvertAndSaveAsync(walletId, usd, eur, 100m, usdRate, eurRate);

        await using ApplicationDbContext readCtx = fixture.CreateDbContext();

        Wallet loaded = await readCtx.Wallets
            .Include(wallet => wallet.Balances)
            .Include(wallet => wallet.Transactions)
            .SingleAsync(wallet => wallet.Id == walletId);

        // Conversion goes through PLN: USD -> PLN -> EUR.
        decimal expectedEur = Math.Round(100m * usdRate / eurRate, 8, MidpointRounding.AwayFromZero);

        loaded.Balances.Single(balance => balance.CurrencyCode == usd).Amount.ShouldBe(0m);
        loaded.Balances.Single(balance => balance.CurrencyCode == eur).Amount.ShouldBe(expectedEur);

        loaded.Transactions.ShouldContain(transaction =>
            transaction.Type == TransactionType.ConversionDebit &&
            transaction.CurrencyCode == usd &&
            transaction.Amount == 100m);

        loaded.Transactions.ShouldContain(transaction =>
            transaction.Type == TransactionType.ConversionCredit &&
            transaction.CurrencyCode == eur &&
            transaction.Amount == expectedEur);
    }

    private async Task<Guid> SeedWalletWithDepositAsync(
        ApplicationDbContext ctx,
        Guid userId,
        CurrencyCode currency,
        decimal amount)
    {
        await using (ctx)
        {
            Wallet wallet = Wallet.Create(userId, "My Wallet", Now).Value;

            wallet.Deposit(currency, amount, Now);

            ctx.Wallets.Add(wallet);
            await ctx.SaveChangesAsync();

            return wallet.Id;
        }
    }

    private async Task WithdrawAndSaveAsync(Guid walletId, CurrencyCode currency, decimal amount)
    {
        await using ApplicationDbContext writeCtx = fixture.CreateDbContext();

        Wallet wallet = await writeCtx.Wallets
            .Include(wallet => wallet.Balances)
            .Include(wallet => wallet.Transactions)
            .SingleAsync(wallet => wallet.Id == walletId);

        wallet.Withdraw(currency, amount, Now);

        await writeCtx.SaveChangesAsync();
    }

    private async Task ConvertAndSaveAsync(
        Guid walletId,
        CurrencyCode from,
        CurrencyCode to,
        decimal amount,
        decimal fromRate,
        decimal toRate)
    {
        await using ApplicationDbContext writeCtx = fixture.CreateDbContext();

        Wallet wallet = await writeCtx.Wallets
            .Include(wallet => wallet.Balances)
            .Include(wallet => wallet.Transactions)
            .SingleAsync(wallet => wallet.Id == walletId);

        wallet.Convert(from, to, amount, fromRate, toRate, Now);

        await writeCtx.SaveChangesAsync();
    }
}
