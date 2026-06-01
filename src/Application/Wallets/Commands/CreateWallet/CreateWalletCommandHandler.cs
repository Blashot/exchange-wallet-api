using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Wallets.Commands.CreateWallet;

internal sealed class CreateWalletCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateWalletCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWalletCommand command, CancellationToken cancellationToken)
    {
        bool userExists = await context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == command.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<Guid>(UserErrors.NotFound(command.UserId));
        }

        Result<Wallet> walletResult = Wallet.Create(command.UserId, command.Name, dateTimeProvider.UtcNow);

        if (walletResult.IsFailure)
        {
            return Result.Failure<Guid>(walletResult.Error);
        }

        context.Wallets.Add(walletResult.Value);

        await context.SaveChangesAsync(cancellationToken);

        return walletResult.Value.Id;
    }
}

