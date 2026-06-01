using Application.Wallets.Commands.Withdraw;
using Domain.Shared;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace Application.UnitTests.Wallets;

public sealed class WithdrawCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTime Now = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly CurrencyCode PLN = CurrencyCode.PLN;

    public WithdrawCommandHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _userContext.UserId.Returns(UserId);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private WithdrawCommandHandler CreateHandler() =>
        new(_context, _userContext, _clock);

    private Wallet CreateFundedWallet(decimal balance = 500m, CurrencyCode? currency = null)
    {
        Wallet wallet = Wallet.Create(UserId, "Test Wallet", Now).Value;
        wallet.Deposit(currency ?? PLN, balance, Now);
        wallet.ClearDomainEvents();
        return wallet;
    }

    private void SetupWallets(params Wallet[] wallets)
    {
        DbSet<Wallet> mockDbSet = wallets.AsQueryable().BuildMockDbSet();
        _context.Wallets.Returns(mockDbSet);
    }
    

    [Fact]
    public async Task Handle_SufficientBalance_ShouldReturnSuccess()
    {
        Wallet wallet = CreateFundedWallet(500m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_SufficientBalance_ShouldReduceBalance()
    {
        Wallet wallet = CreateFundedWallet(500m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 200m);

        await CreateHandler().Handle(command, CancellationToken.None);

        wallet.Balances.Single(b => b.CurrencyCode == PLN).Amount.ShouldBe(300m);
    }

    [Fact]
    public async Task Handle_ExactBalance_ShouldReduceBalanceToZero()
    {
        Wallet wallet = CreateFundedWallet(100m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        wallet.Balances.Single(b => b.CurrencyCode == PLN).Amount.ShouldBe(0m);
    }

    [Fact]
    public async Task Handle_SufficientBalance_ShouldPersistChanges()
    {
        Wallet wallet = CreateFundedWallet(500m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 50m);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    

    [Fact]
    public async Task Handle_InsufficientFunds_ShouldReturnFailure()
    {
        Wallet wallet = CreateFundedWallet(50m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.InsufficientFunds");
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ShouldNotReduceBalance()
    {
        Wallet wallet = CreateFundedWallet(50m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 100m);

        await CreateHandler().Handle(command, CancellationToken.None);

        wallet.Balances.Single(b => b.CurrencyCode == PLN).Amount.ShouldBe(50m);
    }

    [Fact]
    public async Task Handle_InsufficientFunds_ShouldNotCallSaveChanges()
    {
        Wallet wallet = CreateFundedWallet(10m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 100m);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoCurrencyBalance_ShouldReturnInsufficientFunds()
    {
        // Wallet has PLN but command asks for USD
        Wallet wallet = CreateFundedWallet(500m, PLN);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "USD", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.InsufficientFunds");
    }

    [Fact]
    public async Task Handle_WalletNotFound_ShouldReturnNotFoundFailure()
    {
        SetupWallets();
        WithdrawCommand command = new(Guid.NewGuid(), UserId, "PLN", 50m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.NotFound");
    }

    [Fact]
    public async Task Handle_UserContextMismatch_ShouldReturnUnauthorized()
    {
        _userContext.UserId.Returns(Guid.NewGuid());
        Wallet wallet = CreateFundedWallet(500m);
        SetupWallets(wallet);
        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 50m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.Unauthorized");
    }

    [Fact]
    public async Task Handle_ConcurrencyConflict_ShouldReturnConflictError()
    {
        Wallet wallet = CreateFundedWallet(500m);
        SetupWallets(wallet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new DbUpdateConcurrencyException()));

        WithdrawCommand command = new(wallet.Id, UserId, "PLN", 50m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.ConcurrencyConflict");
    }
}


