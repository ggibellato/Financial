using System;

namespace Financial.CashFlow.Domain.Entities;

public sealed record PotChange(Guid? Id, string Name, decimal SplitPercentage, bool IsActive);

public class Pot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public decimal SplitPercentage { get; private set; }

    private Pot() { }

    public static Pot Create(string name, decimal splitPercentage, bool isActive = true)
    {
        Validate(name, splitPercentage);

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            IsActive = isActive,
            SplitPercentage = splitPercentage
        };
    }

    public void Update(string name, decimal splitPercentage, bool isActive)
    {
        Validate(name, splitPercentage);

        Name = name.Trim();
        SplitPercentage = splitPercentage;
        IsActive = isActive;
    }

    public decimal CalculateSplitAmount(decimal totalAmount) =>
        Math.Round(totalAmount * SplitPercentage / 100m, 2, MidpointRounding.AwayFromZero);

    internal static void Validate(string name, decimal splitPercentage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Pot name is required.");
        }

        if (splitPercentage < 0 || splitPercentage > 100)
        {
            throw new ArgumentException("Split percentage must be between 0 and 100.");
        }
    }
}
