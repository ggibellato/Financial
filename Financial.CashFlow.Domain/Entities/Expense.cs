using System;

namespace Financial.CashFlow.Domain.Entities;

public class Expense
{
    public Guid Id { get; private set; }

    private Expense() { }

    public static Expense Create() => new() { Id = Guid.NewGuid() };
}
