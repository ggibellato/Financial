using System;

namespace Financial.Investment.Domain.Entities;

public class Credit
{
    public enum CreditType { Dividend, Rent, JCP }

    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public CreditType Type { get; private set; }
    public decimal Value { get; private set; }

    private Credit() { }

    private Credit(Guid id, DateTime date, CreditType type, decimal value)
    {
        ValidateValue(value);

        Id = id;
        Date = date;
        Type = type;
        Value = value;
    }

    public static Credit Create(DateTime date, CreditType type, decimal value) =>
        new(Guid.NewGuid(), date, type, value);

    public static Credit CreateWithId(Guid id, DateTime date, CreditType type, decimal value) =>
        new(id, date, type, value);

    private static void ValidateValue(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Value must be greater than zero.");
        }
    }
}
