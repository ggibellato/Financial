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
    public Account? PotsAccount { get; private set; }
    public Account? SavingsAccount { get; private set; }

    private InvestmentAccount() { }

    public static InvestmentAccount Create(
        string name,
        bool isActive,
        bool isLiability,
        InvestmentAccountSource source = InvestmentAccountSource.None,
        Account? creditCard = null,
        Account? potsAccount = null,
        Account? savingsAccount = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Investment account name is required.");
        }

        ValidateSourceShape(source, creditCard);
        ValidatePotsAccount(source, null, potsAccount);
        ValidateSavingsAccount(source, null, savingsAccount);

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            IsLiability = isLiability,
            Source = source,
            CreditCard = creditCard,
            PotsAccount = potsAccount,
            SavingsAccount = savingsAccount
        };
    }

    public void Update(
        string name,
        bool isActive,
        bool isLiability,
        InvestmentAccountSource source = InvestmentAccountSource.None,
        Account? creditCard = null,
        Account? potsAccount = null,
        Account? savingsAccount = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Investment account name is required.");
        }

        ValidateSourceShape(source, creditCard);
        ValidatePotsAccount(source, PotsAccount, potsAccount);
        ValidateSavingsAccount(source, SavingsAccount, savingsAccount);

        Name = name;
        IsActive = isActive;
        IsLiability = isLiability;
        Source = source;
        CreditCard = creditCard;
        PotsAccount = potsAccount;
        SavingsAccount = savingsAccount;
    }

    private static void ValidatePotsAccount(InvestmentAccountSource source, Account? current, Account? potsAccount) =>
        ValidateLinkedAccount(source, InvestmentAccountSource.PotsAccountTotal, current, potsAccount, Capability.HasPots, "Pots account");

    private static void ValidateSavingsAccount(InvestmentAccountSource source, Account? current, Account? savingsAccount) =>
        ValidateLinkedAccount(source, InvestmentAccountSource.SavingsAccountBalance, current, savingsAccount, Capability.EarnsInterest, "Savings account");

    private static void ValidateLinkedAccount(
        InvestmentAccountSource source,
        InvestmentAccountSource linkingSource,
        Account? current,
        Account? linkedAccount,
        Capability requiredCapability,
        string accountLabel)
    {
        var sourceLabel = accountLabel.ToLowerInvariant().Replace(' ', '-');

        if (source == linkingSource && linkedAccount is null)
        {
            throw new ArgumentException($"A {sourceLabel}-sourced investment account requires a {accountLabel}.");
        }

        if (source != linkingSource && linkedAccount is not null)
        {
            throw new ArgumentException($"Only a {sourceLabel}-sourced investment account can name a {accountLabel}.");
        }

        if (linkedAccount is not null && !linkedAccount.Has(requiredCapability))
        {
            throw new ArgumentException($"{linkedAccount.Name} is not a {accountLabel}.");
        }

        ActiveAccountRule.EnsureUsable(current, linkedAccount);
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
