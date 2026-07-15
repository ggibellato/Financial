using System;
using System.Collections.Generic;

namespace Financial.Domain.Entities;

public class Asset
{
    public string Name { get; private set; }

    public string ISIN { get; private set; }

    public string Exchange { get; private set; }

    public string Ticker { get; private set; }

    public CountryCode Country { get; private set; } = CountryCode.Unknown;

    public string LocalTypeCode { get; private set; } = string.Empty;

    public GlobalAssetClass Class { get; private set; } = GlobalAssetClass.Unknown;

    public decimal AveragePrice { get; private set; } = 0;

    public decimal Quantity { get; private set; }

    public bool Active => Quantity > 0;

    public PositionType PositionType => Quantity switch
    {
        > 0 => PositionType.Long,
        < 0 => PositionType.Short,
        _ => PositionType.Flat
    };

    private List<Transaction> _transactions = new List<Transaction>();
    public IReadOnlyCollection<Transaction> Transactions { get => _transactions.AsReadOnly(); private set => SetTransactions(value); }
    private void SetTransactions(IReadOnlyCollection<Transaction> data)
    {
        RebuildTransactions(data);
    }

    private void RebuildTransactions(IEnumerable<Transaction> transactions)
    {
        var transactionList = new List<Transaction>(transactions);
        _transactions.Clear();
        AveragePrice = 0;
        Quantity = 0;
        foreach (var transaction in transactionList)
        {
            AddTransaction(transaction);
        }
    }

    private List<Credit> _credits = new List<Credit>();
    public IReadOnlyCollection<Credit> Credits { get => _credits.AsReadOnly(); private set => SetCredits(value); }
    private void SetCredits(IReadOnlyCollection<Credit> data)
    {
        _credits.Clear();
        foreach (var credit in data)
        {
            AddCredit(credit);
        }
    }

    private Asset() { }

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

    public void AddTransaction(Transaction transaction)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException(nameof(transaction));
        }

        if (transaction.Type == Transaction.TransactionType.Buy)
        {
            AveragePrice = (AveragePrice * Quantity + transaction.TotalPrice) / (Quantity + transaction.Quantity);
        }
        Quantity += (transaction.Type == Transaction.TransactionType.Buy
            ? transaction.Quantity : -transaction.Quantity);
        _transactions.Add(transaction);
    }
    public void AddTransactions(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            AddTransaction(transaction);
        }
    }

    public bool UpdateTransaction(Transaction updatedTransaction)
    {
        if (updatedTransaction == null)
        {
            throw new ArgumentNullException(nameof(updatedTransaction));
        }

        EnsureNotEmptyId(updatedTransaction.Id, "Transaction Id is required for update.", nameof(updatedTransaction));

        var index = _transactions.FindIndex(t => t.Id == updatedTransaction.Id);
        if (index < 0)
        {
            return false;
        }

        var transactions = new List<Transaction>(_transactions);
        transactions[index] = updatedTransaction;
        RebuildTransactions(transactions);
        return true;
    }

    public bool RemoveTransaction(Guid transactionId)
    {
        EnsureNotEmptyId(transactionId, "Transaction Id is required for delete.", nameof(transactionId));

        var index = _transactions.FindIndex(t => t.Id == transactionId);
        if (index < 0)
        {
            return false;
        }

        var transactions = new List<Transaction>(_transactions);
        transactions.RemoveAt(index);
        RebuildTransactions(transactions);
        return true;
    }

    public void AddCredit(Credit credit)
    {
        if (credit == null)
        {
            throw new ArgumentNullException(nameof(credit));
        }

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
