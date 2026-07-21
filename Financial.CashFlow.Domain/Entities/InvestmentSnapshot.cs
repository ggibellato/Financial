using System;

namespace Financial.CashFlow.Domain.Entities;

public class InvestmentSnapshot
{
    public Guid Id { get; private set; }

    private InvestmentSnapshot() { }

    public static InvestmentSnapshot Create() => new() { Id = Guid.NewGuid() };
}
