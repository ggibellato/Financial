using System;

namespace Financial.CashFlow.Domain.Entities;

public class MaeLedgerEntry
{
    public Guid Id { get; private set; }

    private MaeLedgerEntry() { }

    public static MaeLedgerEntry Create() => new() { Id = Guid.NewGuid() };
}
