using Domain.Shared;
using Domain.Wallets.Entities;
using Domain.Wallets.Enums;
using Domain.Wallets.Events;
using SharedKernel;

namespace Domain.Wallets;

public sealed class Wallet : Entity
{
    private readonly List<WalletBalance> _balances = [];
    private readonly List<WalletTransaction> _transactions = [];

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid Version { get; private set; }

    public IReadOnlyList<WalletBalance> Balances => _balances.AsReadOnly();

    public IReadOnlyList<WalletTransaction> Transactions => _transactions.AsReadOnly();
    
    private Wallet()
    {
        Name = string.Empty;
    }
    
    public static Result<Wallet> Create(Guid userId, string name, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Wallet>(WalletErrors.NameEmpty);
        }

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            CreatedAt = createdAt,
            Version = Guid.NewGuid()
        };
        
        wallet.Raise(new WalletCreatedDomainEvent(wallet.Id));
        
        return Result.Success(wallet);
    }
    
    public Result Deposit(CurrencyCode currency, decimal amount, DateTime occurredAt)
    {
        if (amount <= 0m)
        {
            return Result.Failure(WalletErrors.InvalidAmount);
        }

        WalletBalance balance = GetOrCreateBalance(currency);
        balance.Add(amount);

        _transactions.Add(
            WalletTransaction.Create(Id, TransactionType.Deposit, currency, amount, occurredAt));

        Raise(new MoneyDepositedDomainEvent(Id, currency, amount));

        return Result.Success();
    }
    
    public Result Withdraw(CurrencyCode currency, decimal amount, DateTime occurredAt)
    {
        if (amount <= 0m)
        {
            return Result.Failure(WalletErrors.InvalidAmount);
        }

        WalletBalance? balance = _balances.SingleOrDefault(b => b.CurrencyCode == currency);

        if (balance is null || !balance.TrySubtract(amount))
        {
            return Result.Failure(WalletErrors.InsufficientFunds);
        }

        _transactions.Add(
            WalletTransaction.Create(Id, TransactionType.Withdrawal, currency, amount, occurredAt));

        Raise(new MoneyWithdrawnDomainEvent(Id, currency, amount));

        return Result.Success();
    }
    
    
    public Result Convert(
        CurrencyCode fromCurrency,
        CurrencyCode toCurrency,
        decimal fromAmount,
        decimal fromMidRate,
        decimal toMidRate,
        DateTime occurredAt)
    {
        if (fromCurrency == toCurrency)
        {
            return Result.Failure(WalletErrors.SameCurrency);
        }

        if (fromAmount <= 0m)
        {
            return Result.Failure(WalletErrors.InvalidAmount);
        }

        //fromCurrency → PLN (PLN per 1 unit)
        decimal plnEquivalent = fromAmount * fromMidRate;

        //PLN → toCurrency (PLN per 1 unit of target)
        decimal toAmount = Math.Round(plnEquivalent / toMidRate, 8, MidpointRounding.AwayFromZero);

        // Debit the source balance
        WalletBalance? sourceBalance = _balances.SingleOrDefault(b => b.CurrencyCode == fromCurrency);

        if (sourceBalance is null || !sourceBalance.TrySubtract(fromAmount))
        {
            return Result.Failure(WalletErrors.InsufficientFunds);
        }

        // Credit the target balance
        WalletBalance targetBalance = GetOrCreateBalance(toCurrency);
        targetBalance.Add(toAmount);

        // Record both in transaction ledger.
        _transactions.Add(
            WalletTransaction.Create(Id, TransactionType.ConversionDebit, fromCurrency, fromAmount, occurredAt));

        _transactions.Add(
            WalletTransaction.Create(Id, TransactionType.ConversionCredit, toCurrency, toAmount, occurredAt));

        Raise(new CurrencyConvertedDomainEvent(Id, fromCurrency, fromAmount, toCurrency, toAmount));

        return Result.Success();
    }
    
    private WalletBalance GetOrCreateBalance(CurrencyCode currency)
    {
        WalletBalance? balance = _balances.SingleOrDefault(b => b.CurrencyCode == currency);

        if (balance is null)
        {
            balance = WalletBalance.Create(Id, currency);
            _balances.Add(balance);
        }

        return balance;
    }
    
}

