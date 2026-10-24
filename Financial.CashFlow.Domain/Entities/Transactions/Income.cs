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
    public bool SplitToReserve { get; private set; } = false;

    private Income() { }

    public static Income Create(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Account? account,
        string? description = null,
        bool splitToReserve = false)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);
        ValidateAccount(account);
        ActiveAccountRule.EnsureUsable(null, account);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            IncomeSource = incomeSource,
            GrossValue = grossValue,
            NetValue = netValue,
            Account = account,
            Description = NormalizeDescription(description),
            SplitToReserve = splitToReserve
        };
    }

    public void UpdateDetails(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Account? account,
        string? description = null,
        bool splitToReserve = false)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);
        ValidateAccount(account);
        ActiveAccountRule.EnsureUsable(Account, account);

        Date = date;
        IncomeSource = incomeSource;
        GrossValue = grossValue;
        NetValue = netValue;
        Account = account;
        Description = NormalizeDescription(description);
        SplitToReserve = splitToReserve;
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

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description;
}
