using System;

namespace Financial.CashFlow.Domain.Entities;

public class ReserveBucket
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public decimal SplitPercentage { get; private set; }

    private ReserveBucket() { }

    public static ReserveBucket Create(string name, decimal splitPercentage, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Reserve bucket name is required.");
        }

        if (splitPercentage < 0 || splitPercentage > 100)
        {
            throw new ArgumentException("Split percentage must be between 0 and 100.");
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            SplitPercentage = splitPercentage
        };
    }

    public decimal CalculateSplitAmount(decimal totalAmount) =>
        Math.Round(totalAmount * SplitPercentage / 100m, 2, MidpointRounding.AwayFromZero);
}
