using System;

namespace Financial.CashFlow.Domain.Entities;

public class ReserveMovement
{
    public Guid Id { get; private set; }
    public ReserveBucket Bucket { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Income? Income { get; private set; }

    private ReserveMovement() { }

    public static ReserveMovement Create(ReserveBucket bucket, decimal amount, DateOnly date, string description, Income? income = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Bucket = bucket,
            Amount = amount,
            Date = date,
            Description = description,
            Income = income
        };

    public void Update(ReserveBucket bucket, decimal amount, DateOnly date, string description)
    {
        Bucket = bucket;
        Amount = amount;
        Date = date;
        Description = description;
    }
}
