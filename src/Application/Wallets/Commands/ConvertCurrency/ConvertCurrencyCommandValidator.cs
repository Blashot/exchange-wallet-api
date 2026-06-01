using FluentValidation;

namespace Application.Wallets.Commands.ConvertCurrency;

internal sealed class ConvertCurrencyCommandValidator : AbstractValidator<ConvertCurrencyCommand>
{
    public ConvertCurrencyCommandValidator()
    {
        RuleFor(c => c.WalletId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.FromCurrencyCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("From currency code must be exactly 3 letters.");
        RuleFor(c => c.ToCurrencyCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("To currency code must be exactly 3 letters.");
        RuleFor(c => c)
            .Must(c => !string.Equals(c.FromCurrencyCode, c.ToCurrencyCode, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source and target currencies must be different.")
            .WithName("FromCurrencyCode");
    }
}

