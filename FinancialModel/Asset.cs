using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Financial.Model;

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
        _operations.Clear();
        foreach (var operation in data)
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

    public void AddCredit(Credit credit)
    {
        _credits.Add(credit);
    }
    public void AddCredits(IEnumerable<Credit> credits)
    {
        _credits.AddRange(credits);
    }
}
