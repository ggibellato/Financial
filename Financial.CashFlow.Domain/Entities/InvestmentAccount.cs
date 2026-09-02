using System;

namespace Financial.CashFlow.Domain.Entities;

public class InvestmentAccount
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsLiability { get; private set; }

    private InvestmentAccount() { }

    public static InvestmentAccount Create(string name, bool isActive, bool isLiability)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Investment account name is required.");
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            IsLiability = isLiability
        };
    }

    public void Update(string name, bool isActive, bool isLiability)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Investment account name is required.");
        }

        Name = name;
        IsActive = isActive;
        IsLiability = isLiability;
    }
}
