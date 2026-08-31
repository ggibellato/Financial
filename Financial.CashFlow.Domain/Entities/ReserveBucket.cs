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
        Validate(name, splitPercentage);

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            SplitPercentage = splitPercentage
        };
    }

    /// <summary>Updates this bucket's fields. Callers own uniqueness checks, since only the
    /// repository can see across every bucket. "Deleting" a bucket is an ordinary Update call with
    /// isActive: false - no hard delete exists, since ReserveMovement holds a permanent,
    /// non-nullable reference to its Bucket.</summary>
    public void Update(string name, decimal splitPercentage, bool isActive)
    {
        Validate(name, splitPercentage);

        Name = name;
        SplitPercentage = splitPercentage;
        IsActive = isActive;
    }

    public decimal CalculateSplitAmount(decimal totalAmount) =>
        Math.Round(totalAmount * SplitPercentage / 100m, 2, MidpointRounding.AwayFromZero);

    private static void Validate(string name, decimal splitPercentage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Reserve bucket name is required.");
        }

        if (splitPercentage < 0 || splitPercentage > 100)
        {
            throw new ArgumentException("Split percentage must be between 0 and 100.");
        }
    }
}
