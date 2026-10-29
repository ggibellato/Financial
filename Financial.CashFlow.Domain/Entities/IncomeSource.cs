using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.Entities;

public class IncomeSource
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public IncomeGroup Group { get; private set; }
    public Account? PotsAccount { get; private set; }

    private IncomeSource() { }

    public static IncomeSource Create(string name, IncomeGroup group, bool isActive = true, Account? potsAccount = null)
    {
        ValidateName(name);
        ValidatePotsAccount(potsAccount);
        ActiveAccountRule.EnsureUsable(null, potsAccount);

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            Group = group,
            PotsAccount = potsAccount
        };
    }

    public void Update(string name, IncomeGroup group, bool isActive, Account? potsAccount)
    {
        ValidateName(name);
        ValidatePotsAccount(potsAccount);
        ActiveAccountRule.EnsureUsable(PotsAccount, potsAccount);

        Name = name;
        Group = group;
        IsActive = isActive;
        PotsAccount = potsAccount;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Income source name is required.");
        }
    }

    private static void ValidatePotsAccount(Account? potsAccount)
    {
        if (potsAccount is not null && !potsAccount.Has(Capability.ReceivesIncomeSplit))
        {
            throw new ArgumentException($"{potsAccount.Name} can't receive an income split.");
        }
    }
}
