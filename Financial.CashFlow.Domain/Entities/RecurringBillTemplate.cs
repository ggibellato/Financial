using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class RecurringBillTemplate
{
    public Guid Id { get; private set; }
    public int DueDay { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public Area Area { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public string? NitNumber { get; private set; }
    public decimal? MinimumWageValue { get; private set; }
    public bool IsActive { get; private set; }

    private RecurringBillTemplate() { }

    public static RecurringBillTemplate Create(
        int dueDay,
        string description,
        decimal value,
        Area area,
        string note,
        string? nitNumber,
        decimal? minimumWageValue) =>
        new()
        {
            Id = Guid.NewGuid(),
            DueDay = dueDay,
            Description = description,
            Value = value,
            Area = area,
            Note = note,
            NitNumber = nitNumber,
            MinimumWageValue = minimumWageValue,
            IsActive = true
        };
}
