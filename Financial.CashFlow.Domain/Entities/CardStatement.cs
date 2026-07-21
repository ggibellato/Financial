using System;

namespace Financial.CashFlow.Domain.Entities;

public class CardStatement
{
    public Guid Id { get; private set; }

    private CardStatement() { }

    public static CardStatement Create() => new() { Id = Guid.NewGuid() };
}
