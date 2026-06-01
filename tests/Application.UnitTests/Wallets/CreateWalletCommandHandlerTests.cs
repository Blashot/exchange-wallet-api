using Application.Wallets.Commands.CreateWallet;
using Domain.Users;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace Application.UnitTests.Wallets;

public sealed class CreateWalletCommandHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private static readonly DateTime Now = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateWalletCommandHandlerTests()
    {
        _clock.UtcNow.Returns(Now);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private CreateWalletCommandHandler CreateHandler() =>
        new(_context, _clock);

    private void SetupExistingUser()
    {
        List<User> users =
        [
            new User { Id = UserId, Email = "user@test.com", FirstName = "A", LastName = "B", PasswordHash = "x" }
        ];
        DbSet<User> mockUsers = users.AsQueryable().BuildMockDbSet();
        DbSet<Wallet> mockWallets = new List<Wallet>().AsQueryable().BuildMockDbSet();
        _context.Users.Returns(mockUsers);
        _context.Wallets.Returns(mockWallets);
    }

    private void SetupNoUsers()
    {
        DbSet<User> mockUsers = new List<User>().AsQueryable().BuildMockDbSet();
        _context.Users.Returns(mockUsers);
    }



    [Fact]
    public async Task Handle_UserExists_ShouldReturnSuccessWithNewWalletId()
    {
        SetupExistingUser();
        CreateWalletCommand command = new(UserId, "My Wallet");

        Result<Guid> result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_UserExists_ShouldPersistWallet()
    {
        SetupExistingUser();
        CreateWalletCommand command = new(UserId, "My Wallet");

        await CreateHandler().Handle(command, CancellationToken.None);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserExists_ShouldAddWalletToContext()
    {
        SetupExistingUser();
        DbSet<Wallet> walletDbSet = _context.Wallets;
        CreateWalletCommand command = new(UserId, "My Wallet");

        await CreateHandler().Handle(command, CancellationToken.None);

        walletDbSet.Received(1).Add(Arg.Any<Wallet>());
    }



    [Fact]
    public async Task Handle_UserDoesNotExist_ShouldReturnNotFoundFailure()
    {
        SetupNoUsers();
        CreateWalletCommand command = new(UserId, "My Wallet");

        Result<Guid> result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.NotFound");
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ShouldNotCallSaveChanges()
    {
        SetupNoUsers();
        CreateWalletCommand command = new(UserId, "My Wallet");

        await CreateHandler().Handle(command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyWalletName_ShouldReturnDomainFailure()
    {
        SetupExistingUser();
        CreateWalletCommand command = new(UserId, "   ");

        Result<Guid> result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Wallets.NameEmpty");
    }

    [Fact]
    public async Task Handle_EmptyWalletName_ShouldNotCallSaveChanges()
    {
        SetupExistingUser();
        CreateWalletCommand command = new(UserId, "");

        await CreateHandler().Handle(command, CancellationToken.None);

        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

