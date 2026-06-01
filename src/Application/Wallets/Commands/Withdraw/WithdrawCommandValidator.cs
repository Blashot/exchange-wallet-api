using FluentValidation;

namespace Application.Wallets.Commands.Withdraw;

internal sealed class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
{
    public WithdrawCommandValidator()
    {
        RuleFor(c => c.WalletId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.CurrencyCode).NotEmpty().Length(3).Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must be exactly 3 letters.");
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}

