using Application.Wallets.Commands.Deposit;
using Domain.Shared;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace Application.UnitTests.Wallets;

public sealed class DepositCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    private static readonly DateTime Now = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    public DepositCommandHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _userContext.UserId.Returns(UserId);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private DepositCommandHandler CreateHandler() =>
        new(_context, _userContext, _clock);

    private Wallet CreateWalletForUser(Guid? userId = null)
    {
        Wallet wallet = Wallet.Create(userId ?? UserId, "Test Wallet", Now).Value;
        wallet.ClearDomainEvents();
        return wallet;
    }

    private void SetupWallets(params Wallet[] wallets)
    {
        DbSet<Wallet> mockDbSet = wallets.AsQueryable().BuildMockDbSet();
        _context.Wallets.Returns(mockDbSet);
    }



    [Fact]
    public async Task Handle_ValidDeposit_ShouldReturnSuccess()
    {
        Wallet wallet = CreateWalletForUser();
        SetupWallets(wallet);
        DepositCommand command = new(wallet.Id, UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidDeposit_ShouldPersistChanges()
    {
        Wallet wallet = CreateWalletForUser();
        SetupWallets(wallet);
        DepositCommand command = new(wallet.Id, UserId, "PLN", 100m);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidDeposit_ShouldUpdateWalletBalance()
    {
        Wallet wallet = CreateWalletForUser();
        SetupWallets(wallet);
        DepositCommand command = new(wallet.Id, UserId, "PLN", 250m);

        await CreateHandler().Handle(command, CancellationToken.None);

        wallet.Balances.Single(b => b.CurrencyCode == CurrencyCode.PLN).Amount.ShouldBe(250m);
    }



    [Fact]
    public async Task Handle_WalletNotFound_ShouldReturnNotFoundFailure()
    {
        SetupWallets(); // empty
        DepositCommand command = new(Guid.NewGuid(), UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.NotFound");
    }
    

    [Fact]
    public async Task Handle_UserContextDoesNotMatchCommand_ShouldReturnUnauthorized()
    {
        _userContext.UserId.Returns(Guid.NewGuid()); // different user
        Wallet wallet = CreateWalletForUser(UserId);
        SetupWallets(wallet);
        DepositCommand command = new(wallet.Id, UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.Unauthorized");
    }

    [Fact]
    public async Task Handle_WalletBelongsToDifferentUser_ShouldReturnUnauthorized()
    {
        var otherUserId = Guid.NewGuid();
        Wallet wallet = CreateWalletForUser(otherUserId); // owned by someone else
        SetupWallets(wallet);
        DepositCommand command = new(wallet.Id, UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.Unauthorized");
    }
    

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.001)]
    public async Task Handle_InvalidAmount_ShouldReturnDomainFailure(decimal amount)
    {
        Wallet wallet = CreateWalletForUser();
        SetupWallets(wallet);
        DepositCommand command = new(wallet.Id, UserId, "PLN", amount);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.InvalidAmount");
    }
    

    [Fact]
    public async Task Handle_ConcurrencyConflict_ShouldReturnConflictError()
    {
        Wallet wallet = CreateWalletForUser();
        SetupWallets(wallet);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new DbUpdateConcurrencyException()));

        DepositCommand command = new(wallet.Id, UserId, "PLN", 100m);

        Result result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.ConcurrencyConflict");
    }
}
