using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class Income : IAccountTransaction
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public IncomeSource IncomeSource { get; private set; } = null!;
    public decimal? GrossValue { get; private set; }
    public decimal NetValue { get; private set; }
    public Account? Account { get; private set; }
    public string? Description { get; private set; }
    public Account? PotsAccount { get; private set; }

    private Income() { }

    public static Income Create(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Account? account,
        string? description = null,
        Account? potsAccount = null)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);
        ValidateAccount(account);
        ActiveAccountRule.EnsureUsable(null, account);
        ValidatePotsAccount(incomeSource, potsAccount);
        ActiveAccountRule.EnsureUsable(null, potsAccount);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            IncomeSource = incomeSource,
            GrossValue = grossValue,
            NetValue = netValue,
            Account = account,
            Description = NormalizeDescription(description),
            PotsAccount = potsAccount
        };
    }

    public void UpdateDetails(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Account? account,
        string? description = null,
        Account? potsAccount = null)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);
        ValidateAccount(account);
        ActiveAccountRule.EnsureUsable(Account, account);
        ValidatePotsAccount(incomeSource, potsAccount);
        ActiveAccountRule.EnsureUsable(PotsAccount, potsAccount);

        Date = date;
        IncomeSource = incomeSource;
        GrossValue = grossValue;
        NetValue = netValue;
        Account = account;
        Description = NormalizeDescription(description);
        PotsAccount = potsAccount;
    }

    private static void ValidateValues(decimal? grossValue, decimal netValue)
    {
        if (netValue < 0)
        {
            throw new ArgumentException("Net value cannot be negative.");
        }
    }

    private static void ValidateIncomeSource(IncomeSource incomeSource)
    {
        if (incomeSource is null)
        {
            throw new ArgumentException("Income source is required.");
        }
    }

    public decimal ContributionTo(Account account) => Account?.Id == account.Id ? NetValue : 0m;

    private static void ValidateAccount(Account? account)
    {
        if (account is not null && !account.Has(Capability.ReceivesIncome))
        {
            throw new ArgumentException($"Account '{account.Name}' cannot receive income.");
        }
    }

    private static void ValidatePotsAccount(IncomeSource incomeSource, Account? potsAccount)
    {
        if (potsAccount is null)
        {
            return;
        }

        if (incomeSource.PotsAccount is null)
        {
            throw new ArgumentException($"{incomeSource.Name} doesn't split income to pots.");
        }

        if (!potsAccount.Has(Capability.ReceivesIncomeSplit))
        {
            throw new ArgumentException($"{potsAccount.Name} can't receive an income split.");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description;
}
