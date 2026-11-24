using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class ExternalPayment : IAccountTransaction
{
    public const string AmountMessage = "Enter an amount above 0 with at most 2 decimal places.";
    public const string DescriptionMessage = "Enter a description.";
    public const string DirectionMessage = "Choose whether money is paid in or out.";

    public Guid Id { get; private set; }
    public PaymentDirection Direction { get; private set; }
    public DateOnly Date { get; private set; }
    public Account Account { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private ExternalPayment() { }

    public static ExternalPayment Create(PaymentDirection direction, DateOnly date, Account account, decimal amount, string description)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentException(DirectionMessage);
        }

        EnsureAccountTakes(direction, account);
        ActiveAccountRule.EnsureUsable(null, account);
        var payment = new ExternalPayment { Id = Guid.NewGuid(), Direction = direction, Account = account };
        payment.UpdateDetails(date, amount, description);
        return payment;
    }

    public void UpdateDetails(DateOnly date, decimal amount, string description)
    {
        if (amount <= 0 || MoneyRounding.ToCents(amount) != amount)
        {
            throw new ArgumentException(AmountMessage);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(DescriptionMessage);
        }

        Date = date;
        Amount = amount;
        Description = description.Trim();
    }

    public bool PaysInto(Account account) => Direction == PaymentDirection.In && Account.Id == account.Id;

    public decimal ContributionTo(Account account) =>
        Account.Id != account.Id ? 0m
        : Direction == PaymentDirection.In ? Amount
        : -Amount;

    private static void EnsureAccountTakes(PaymentDirection direction, Account account)
    {
        if (!account.Has(Capability.HasExternalPayments))
        {
            throw new ArgumentException($"{account.Name} can't take a pay-in or pay-out.");
        }

        if (direction == PaymentDirection.In && account.ReceivesOnlyRoundUps)
        {
            throw new ArgumentException($"{account.Name} only receives round-ups.");
        }
    }
}
