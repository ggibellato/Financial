using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Financial.Domain.Entities;

public class Asset
{
    [JsonInclude]
    public string Name { get; private set; }

    [JsonInclude]
    public string ISIN { get; private set; }

    [JsonInclude]
    public string Exchange{ get; private set; }

    [JsonInclude]
    public string Ticker { get; private set; }

    [JsonIgnore]
    public decimal AvargePrice { get; private set; } = 0;

    [JsonIgnore]
    public decimal Quantity { get; private set; }

    [JsonIgnore]
    public bool Active
    {
        get
        {
            return Quantity > 0;
        }
    }

    private List<Operation> _operations = new List<Operation>();
    [JsonInclude]
    public IReadOnlyCollection<Operation> Operations { get => _operations.AsReadOnly(); set => SetOperations(value); }
    private void SetOperations(IReadOnlyCollection<Operation> data)
    {
        RebuildOperations(data);
    }

    private void RebuildOperations(IEnumerable<Operation> operations)
    {
        var operationList = new List<Operation>(operations);
        _operations.Clear();
        AvargePrice = 0;
        Quantity = 0;
        foreach (var operation in operationList)
        {
            AddOperation(operation);
        }
    }

    private List<Credit> _credits = new List<Credit>();
    [JsonInclude]
    public IReadOnlyCollection<Credit> Credits { get => _credits.AsReadOnly(); set => SetCredits(value); }
    private void SetCredits(IReadOnlyCollection<Credit> data)
    {
        _credits.Clear();
        _credits.AddRange(data);
    }

    [JsonConstructor]
    private Asset() {}

    private Asset(string name, string isin, string exchange, string ticker) : this()
    {
        Name = name;
        ISIN = isin;
        Exchange = exchange;
        Ticker = ticker;
    }

    public static Asset Create(string name, string isin, string exchange, string ticker) => new(name, isin, exchange, ticker);

    public void AddOperation(Operation operation)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        operation.EnsureId();
        if (operation.Type == Operation.OperationType.Buy)
        {
            AvargePrice = (AvargePrice * Quantity + operation.TotalPrice) / (Quantity + operation.Quantity);
        }
        Quantity += (operation.Type == Operation.OperationType.Buy 
            ? operation.Quantity : -operation.Quantity);
        _operations.Add(operation);
    }
    public void AddOperations(IEnumerable<Operation> operations)
    {
        foreach (var operation in operations)
        {
            AddOperation(operation);
        }
    }

    public bool UpdateOperation(Operation updatedOperation)
    {
        if (updatedOperation == null)
        {
            throw new ArgumentNullException(nameof(updatedOperation));
        }

        if (updatedOperation.Id == Guid.Empty)
        {
            throw new ArgumentException("Operation Id is required for update.", nameof(updatedOperation));
        }

        var index = _operations.FindIndex(op => op.Id == updatedOperation.Id);
        if (index < 0)
        {
            return false;
        }

        var operations = new List<Operation>(_operations);
        operations[index] = updatedOperation;
        RebuildOperations(operations);
        return true;
    }

    public bool RemoveOperation(Guid operationId)
    {
        if (operationId == Guid.Empty)
        {
            throw new ArgumentException("Operation Id is required for delete.", nameof(operationId));
        }

        var index = _operations.FindIndex(op => op.Id == operationId);
        if (index < 0)
        {
            return false;
        }

        var operations = new List<Operation>(_operations);
        operations.RemoveAt(index);
        RebuildOperations(operations);
        return true;
    }

    public void AddCredit(Credit credit)
    {
        _credits.Add(credit);
    }
    public void AddCredits(IEnumerable<Credit> credits)
    {
        _credits.AddRange(credits);
    }
}
