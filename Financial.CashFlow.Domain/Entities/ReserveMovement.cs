using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class ReserveMovement
{
    public Guid Id { get; private set; }
    public ReserveBucket Bucket { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private ReserveMovement() { }

    public static ReserveMovement Create(ReserveBucket bucket, decimal amount, DateOnly date, string description) =>
        new()
        {
            Id = Guid.NewGuid(),
            Bucket = bucket,
            Amount = amount,
            Date = date,
            Description = description
        };
}
