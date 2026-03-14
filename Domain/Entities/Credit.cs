using System;
using System.Text.Json.Serialization;

namespace Financial.Domain.Entities;

public class Credit
{
    public enum CreditType { Dividend, Rent }

    [JsonInclude]
    public Guid Id { get; private set; }
    [JsonInclude]
    public DateTime Date { get; private set; }
    [JsonInclude]
    public CreditType Type { get; private set; }
    [JsonInclude]
    public decimal Value { get; private set; }

    [JsonConstructor]
    private Credit() { }

    private Credit(Guid id, DateTime date, CreditType type, decimal value)
    {
        Id = id;
        Date = date;
        Type = type;
        Value = value;
    }

    public static Credit Create(DateTime date, CreditType type, decimal value) =>
        new(Guid.NewGuid(), date, type, value);

    public static Credit CreateWithId(Guid id, DateTime date, CreditType type, decimal value) =>
        new(id, date, type, value);

    public void EnsureId()
    {
        if (Id == Guid.Empty)
        {
            Id = Guid.NewGuid();
        }
    }
}
