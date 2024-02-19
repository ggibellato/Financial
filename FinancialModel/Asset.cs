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

    [JsonInclude]
    public decimal Quantity{ get; private set; }

    [JsonInclude]
    public bool Active
    {
        get
        {
            return Quantity > 0;
        }
    }

    private List<Operation> _operations = new List<Operation>();
    [JsonInclude]
    public IReadOnlyCollection<Operation> Operations { get { return _operations.AsReadOnly(); } }

    private List<Credit> _credits = new List<Credit>();
    [JsonInclude]
    public IReadOnlyCollection<Credit> Credits { get { return _credits.AsReadOnly(); } }


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
