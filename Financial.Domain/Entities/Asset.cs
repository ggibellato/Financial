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

    [JsonInclude]
    public CountryCode Country { get; private set; } = CountryCode.Unknown;

    [JsonInclude]
    public string LocalTypeCode { get; private set; } = string.Empty;

    [JsonInclude]
    public GlobalAssetClass Class { get; private set; } = GlobalAssetClass.Unknown;

    [JsonIgnore]
    public decimal AvargePrice { get; private set; } = 0;

    [JsonIgnore]
    public decimal Quantity { get; private set; }

    [JsonIgnore]
    public bool Active => Quantity > 0;

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
        foreach (var credit in data)
        {
            AddCredit(credit);
        }
    }

    [JsonConstructor]
    private Asset() {}

    private Asset(string name, string isin, string exchange, string ticker, CountryCode country, string localTypeCode, GlobalAssetClass assetClass) : this()
    {
        Name = name;
        ISIN = isin;
        Exchange = exchange;
        Ticker = ticker;
        Country = country;
        LocalTypeCode = NormalizeLocalTypeCode(localTypeCode);
        Class = assetClass;
    }

    public static Asset Create(string name, string isin, string exchange, string ticker) =>
        new(name, isin, exchange, ticker, CountryCode.Unknown, string.Empty, GlobalAssetClass.Unknown);

    public static Asset Create(string name, string isin, string exchange, string ticker, CountryCode country, string localTypeCode)
    {
        var normalizedLocalTypeCode = NormalizeLocalTypeCode(localTypeCode);
        var assetClass = GlobalAssetClassMapping.Resolve(country, normalizedLocalTypeCode);
        return new Asset(name, isin, exchange, ticker, country, normalizedLocalTypeCode, assetClass);
    }

    public static Asset Create(
        string name,
        string isin,
        string exchange,
        string ticker,
        CountryCode country,
        string localTypeCode,
        GlobalAssetClass assetClass)
    {
        var normalizedLocalTypeCode = NormalizeLocalTypeCode(localTypeCode);
        return new Asset(name, isin, exchange, ticker, country, normalizedLocalTypeCode, assetClass);
    }

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

        EnsureNotEmptyId(updatedOperation.Id, "Operation Id is required for update.", nameof(updatedOperation));

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
        EnsureNotEmptyId(operationId, "Operation Id is required for delete.", nameof(operationId));

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
        if (credit == null)
        {
            throw new ArgumentNullException(nameof(credit));
        }

        credit.EnsureId();
        _credits.Add(credit);
    }

    public bool UpdateCredit(Credit updatedCredit)
    {
        if (updatedCredit == null)
        {
            throw new ArgumentNullException(nameof(updatedCredit));
        }

        EnsureNotEmptyId(updatedCredit.Id, "Credit Id is required for update.", nameof(updatedCredit));

        var index = _credits.FindIndex(credit => credit.Id == updatedCredit.Id);
        if (index < 0)
        {
            return false;
        }

        _credits[index] = updatedCredit;
        return true;
    }

    public bool RemoveCredit(Guid creditId)
    {
        EnsureNotEmptyId(creditId, "Credit Id is required for delete.", nameof(creditId));

        var index = _credits.FindIndex(credit => credit.Id == creditId);
        if (index < 0)
        {
            return false;
        }

        _credits.RemoveAt(index);
        return true;
    }
    public void AddCredits(IEnumerable<Credit> credits)
    {
        foreach (var credit in credits)
        {
            AddCredit(credit);
        }
    }

    private static void EnsureNotEmptyId(Guid id, string message, string paramName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    private static string NormalizeLocalTypeCode(string localTypeCode)
    {
        return string.IsNullOrWhiteSpace(localTypeCode) ? string.Empty : localTypeCode.Trim();
    }
}
