using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class IncomeSource
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public IncomeGroup Group { get; private set; }
    public bool AutoSplitToReserve { get; private set; } = false;

    private IncomeSource() { }

    public static IncomeSource Create(string name, IncomeGroup group, bool isActive = true, bool autoSplitToReserve = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Income source name is required.");
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            Group = group,
            AutoSplitToReserve = autoSplitToReserve
        };
    }
}
