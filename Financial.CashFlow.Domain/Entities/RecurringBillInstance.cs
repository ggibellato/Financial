using System;

namespace Financial.CashFlow.Domain.Entities;

public class RecurringBillInstance
{
    public Guid Id { get; private set; }

    private RecurringBillInstance() { }

    public static RecurringBillInstance Create() => new() { Id = Guid.NewGuid() };
}
