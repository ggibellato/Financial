using System;
using System.Text.Json.Serialization;

namespace Financial.Model;

public class Credit
{
    public enum CreditType { Dividend, Rent }

    [JsonInclude]
    public DateTime Date { get; private set; }
    [JsonInclude]
    public CreditType Type { get; private set; }
    [JsonInclude]
    public decimal Value { get; private set; }

    [JsonConstructor]
    private Credit() { }

    private Credit(DateTime date, CreditType type, decimal value)
    {
        Date = date;
        Type = type;
        Value = value;
    }

    public static Credit Create(DateTime date, CreditType type, decimal value) =>
        new(date, type, value);
}
