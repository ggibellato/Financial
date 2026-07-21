using System;

namespace Financial.CashFlow.Domain.Entities;

public class RecurringBillTemplate
{
    public Guid Id { get; private set; }

    private RecurringBillTemplate() { }

    public static RecurringBillTemplate Create() => new() { Id = Guid.NewGuid() };
}
