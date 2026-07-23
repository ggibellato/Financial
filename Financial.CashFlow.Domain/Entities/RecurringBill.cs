using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class RecurringBill
{
    public Guid Id { get; private set; }
    public int DueDay { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public Area Area { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public string? NitNumber { get; private set; }
    public decimal? MinimumWageValue { get; private set; }
    public BillStatus Status { get; private set; }

    private RecurringBill() { }

    public static RecurringBill Create(
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
            Status = BillStatus.Unset
        };

    public void Update(BillStatus status, decimal value)
    {
        Status = status;
        Value = value;
    }

    public void ResetToUnset() => Status = BillStatus.Unset;
}
