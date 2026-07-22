using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class RecurringBillInstance
{
    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal Value { get; private set; }
    public BillStatus Status { get; private set; }

    private RecurringBillInstance() { }

    public static RecurringBillInstance Create(Guid templateId, int year, int month, decimal value) =>
        new()
        {
            Id = Guid.NewGuid(),
            TemplateId = templateId,
            Year = year,
            Month = month,
            Value = value,
            Status = BillStatus.Unset
        };

    public void Update(BillStatus status, decimal value)
    {
        Status = status;
        Value = value;
    }
}
