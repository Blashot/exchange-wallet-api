using Application.Wallets.Commands.ConvertCurrency;
using Domain.ExchangeRates;
using Domain.Shared;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace Application.UnitTests.Wallets;

public sealed class ConvertCurrencyCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTime Now = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly CurrencyCode PLN = CurrencyCode.PLN;
    private static readonly CurrencyCode USD = new("USD");
    private static readonly CurrencyCode EUR = new("EUR");

    private const decimal UsdRate = 4.00m;
    private const decimal EurRate = 4.50m;

    public ConvertCurrencyCommandHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _userContext.UserId.Returns(UserId);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private ConvertCurrencyCommandHandler CreateHandler() =>
        new(_context, _userContext, _clock);

    private Wallet CreateWalletWithBalance(CurrencyCode currency, decimal amount)
    {
        Wallet wallet = Wallet.Create(UserId, "Test Wallet", Now).Value;
        wallet.Deposit(currency, amount, Now);
        wallet.ClearDomainEvents();
        return wallet;
    }

    private static ExchangeRateTable CreateRateTable(
        DateOnly date,
        IEnumerable<(CurrencyCode Code, decimal Rate)> rates)
    {
        IEnumerable<(CurrencyCode, string, decimal)> rateData =
            rates.Select(r => (r.Code, r.Code.Value + " Currency", r.Rate));
        return ExchangeRateTable.Create("001/B/NBP/2026", date, DateTime.UtcNow, rateData);
    }

    private static ExchangeRateTable DefaultRateTable() =>
        CreateRateTable(DateOnly.FromDateTime(DateTime.UtcNow), [(USD, UsdRate), (EUR, EurRate)]);

    private void SetupWallets(params Wallet[] wallets)
    {
        DbSet<Wallet> mockDbSet = wallets.AsQueryable().BuildMockDbSet();
        _context.Wallets.Returns(mockDbSet);
    }

    private void SetupRateTables(params ExchangeRateTable[] tables)
    {
        DbSet<ExchangeRateTable> mockDbSet = tables.AsQueryable().BuildMockDbSet();
        _context.ExchangeRateTables.Returns(mockDbSet);
    }

    [Fact]
    public async Task Handle_ValidConversion_ShouldReturnSuccess()
    {
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        SetupWallets(wallet);
        SetupRateTables(DefaultRateTable());

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidConversion_ShouldPersistChanges()
    {
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        SetupWallets(wallet);
        SetupRateTables(DefaultRateTable());

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsdToEur_ShouldCreditCorrectAmount()
    {
        // 100 USD ? PLN (100 * 4.00 = 400) ? EUR (400 / 4.50)
        decimal expectedEur = Math.Round(100m * UsdRate / EurRate, 8, MidpointRounding.AwayFromZero);
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        SetupWallets(wallet);
        SetupRateTables(DefaultRateTable());

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        await CreateHandler().Handle(command, CancellationToken.None);

        wallet.Balances.Single(b => b.CurrencyCode == EUR).Amount.ShouldBe(expectedEur);
    }

    [Fact]
    public async Task Handle_PlnToUsd_ShouldCreditCorrectAmount()
    {
        decimal expectedUsd = Math.Round(100m * 1.0m / UsdRate, 8, MidpointRounding.AwayFromZero);
        Wallet wallet = CreateWalletWithBalance(PLN, 500m);
        SetupWallets(wallet);
        SetupRateTables(DefaultRateTable());

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "PLN", "USD", 100m);

        await CreateHandler().Handle(command, CancellationToken.None);

        wallet.Balances.Single(b => b.CurrencyCode == USD).Amount.ShouldBe(expectedUsd);
    }

    [Fact]
    public async Task Handle_MultipleRateTables_ShouldUseLatestByPublicationDate()
    {
        // Check: Older table has a wrong rate; newer table has the correct rate.
        ExchangeRateTable oldTable = CreateRateTable(
            new DateOnly(2026, 1, 1),
            [(USD, 999m), (EUR, 999m)]);

        ExchangeRateTable newTable = CreateRateTable(
            new DateOnly(2026, 5, 29),
            [(USD, UsdRate), (EUR, EurRate)]);

        decimal expectedEur = Math.Round(100m * UsdRate / EurRate, 8, MidpointRounding.AwayFromZero);
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        SetupWallets(wallet);
        SetupRateTables(oldTable, newTable);

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        await CreateHandler().Handle(command, CancellationToken.None);

        //correct (latest) table used, EUR amount matches expected
        wallet.Balances.Single(b => b.CurrencyCode == EUR).Amount.ShouldBe(expectedEur);
    }

    [Fact]
    public async Task Handle_NoRateTables_ShouldReturnNoRatesAvailableFailure()
    {
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        SetupWallets(wallet);
        SetupRateTables(); // empty

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ExchangeRates.NoRatesAvailable");
    }

    [Fact]
    public async Task Handle_FromCurrencyNotInTable_ShouldReturnCurrencyNotFoundFailure()
    {
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        // USD is not in the table but only EUR
        ExchangeRateTable table = CreateRateTable(DateOnly.FromDateTime(DateTime.UtcNow), [(EUR, EurRate)]);
        SetupWallets(wallet);
        SetupRateTables(table);

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ExchangeRates.CurrencyNotFound");
    }

    [Fact]
    public async Task Handle_ToCurrencyNotInTable_ShouldReturnCurrencyNotFoundFailure()
    {
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        // EUR is not in the table but only USD
        ExchangeRateTable table = CreateRateTable(DateOnly.FromDateTime(DateTime.UtcNow), [(USD, UsdRate)]);
        SetupWallets(wallet);
        SetupRateTables(table);

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ExchangeRates.CurrencyNotFound");
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ShouldReturnFailure()
    {
        Wallet wallet = CreateWalletWithBalance(USD, 10m);
        SetupWallets(wallet);
        SetupRateTables(DefaultRateTable());

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.InsufficientFunds");
    }

    [Fact]
    public async Task Handle_WalletNotFound_ShouldReturnNotFoundFailure()
    {
        SetupWallets(); // empty
        SetupRateTables(DefaultRateTable());

        ConvertCurrencyCommand command = new(Guid.NewGuid(), UserId, "USD", "EUR", 50m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.NotFound");
    }

    [Fact]
    public async Task Handle_UserContextMismatch_ShouldReturnUnauthorized()
    {
        _userContext.UserId.Returns(Guid.NewGuid());
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        SetupWallets(wallet);
        SetupRateTables(DefaultRateTable());

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 50m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.Unauthorized");
    }

    [Fact]
    public async Task Handle_ConcurrencyConflict_ShouldReturnConflictError()
    {
        Wallet wallet = CreateWalletWithBalance(USD, 100m);
        SetupWallets(wallet);
        SetupRateTables(DefaultRateTable());
        _context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new DbUpdateConcurrencyException()));

        ConvertCurrencyCommand command = new(wallet.Id, UserId, "USD", "EUR", 50m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.ConcurrencyConflict");
    }
}
