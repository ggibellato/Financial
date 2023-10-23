using System;
using System.Text.Json.Serialization;

namespace Financial.Model;

public class Operation
{
    public enum OperationType { Buy, Sell }

    [JsonInclude]
    public DateTime Date { get; private set; }
    [JsonInclude]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationType Type { get; private set; }
    [JsonInclude]
    public decimal Quantity { get; private set; }
    [JsonInclude]
    public decimal UnitPrice { get; private set; }
    [JsonInclude]
    public decimal Fees { get; private set; }

    [JsonConstructor]
    private Operation() { }

    private Operation(DateTime date, OperationType type, decimal quantity, decimal unitPrice, decimal fees)
    {
        Date = date;
        Type = type;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Fees = fees;
    }

    public static Operation Create(DateTime date, OperationType type, decimal quantity, decimal unitPrice, decimal fees) =>
        new(date, type, quantity, unitPrice, fees);

}