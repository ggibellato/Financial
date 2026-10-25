using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.Entities;

public class InvestmentAccount
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsLiability { get; private set; }
    public InvestmentAccountSource Source { get; private set; }
    public Account? CreditCard { get; private set; }

    private InvestmentAccount() { }

    public static InvestmentAccount Create(
        string name,
        bool isActive,
        bool isLiability,
        InvestmentAccountSource source = InvestmentAccountSource.None,
        Account? creditCard = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Investment account name is required.");
        }

        ValidateSourceShape(source, creditCard);

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            IsLiability = isLiability,
            Source = source,
            CreditCard = creditCard
        };
    }

    public void Update(
        string name,
        bool isActive,
        bool isLiability,
        InvestmentAccountSource source = InvestmentAccountSource.None,
        Account? creditCard = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Investment account name is required.");
        }

        ValidateSourceShape(source, creditCard);

        Name = name;
        IsActive = isActive;
        IsLiability = isLiability;
        Source = source;
        CreditCard = creditCard;
    }

    private static void ValidateSourceShape(InvestmentAccountSource source, Account? creditCard)
    {
        if (source == InvestmentAccountSource.CreditCard && creditCard is null)
        {
            throw new ArgumentException("A credit-card-sourced investment account requires a linked credit card.");
        }

        if (source != InvestmentAccountSource.CreditCard && creditCard is not null)
        {
            throw new ArgumentException("Only a credit-card-sourced investment account can have a linked credit card.");
        }

        if (creditCard is null)
        {
            return;
        }

        CardAccountRule.EnsureIsCard(creditCard);

        if (!creditCard.IsActive)
        {
            throw new ArgumentException(
                $"Credit card '{creditCard.Name}' is inactive and cannot be used for new entries.");
        }
    }
}
