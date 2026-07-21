using System;

namespace Financial.CashFlow.Domain.Entities;

public class ReserveMovement
{
    public Guid Id { get; private set; }

    private ReserveMovement() { }

    public static ReserveMovement Create() => new() { Id = Guid.NewGuid() };
}
