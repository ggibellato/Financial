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

    /// <summary>Updates this income source's fields. Callers own uniqueness checks, since only the
    /// repository can see across every income source.</summary>
    public void Update(string name, IncomeGroup group, bool isActive, bool autoSplitToReserve)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Income source name is required.");
        }

        Name = name;
        Group = group;
        IsActive = isActive;
        AutoSplitToReserve = autoSplitToReserve;
    }
}
