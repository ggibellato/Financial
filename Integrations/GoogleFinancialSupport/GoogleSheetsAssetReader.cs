using Financial.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal sealed class GoogleSheetsAssetReader
{
    private readonly GoogleService _service;

    internal GoogleSheetsAssetReader(GoogleService service)
    {
        _service = service;
    }

    internal async Task<(string isin, string exchangeId, string ticker)> GetAssetDataAsync(string fileId, string spreadSheetName)
    {
        var values = await _service.GetSpreadSheetDataAsync(fileId, $"{spreadSheetName}!Q2:S2");
        string isin = string.Empty;
        string exchangeId = string.Empty;
        string ticker = string.Empty;
        try
        {
            if (values is not null)
            {
                var row = values.FirstOrDefault();
                exchangeId = (string)row[0];
                ticker = (string)row[1];
                isin = (string)row[2];
            }
        }
        catch (InvalidCastException) { }
        catch (ArgumentOutOfRangeException) { }
        return (isin, exchangeId, ticker);
    }

    internal async Task<List<Transaction>> ReadTransactionsAsync(string fileId, string spreadSheetName)
    {
        var transactions = new List<Transaction>();
        var values = await _service.GetSpreadSheetDataAsync(fileId, $"{spreadSheetName}!A3:G");
        var previousDate = 0L;

        foreach (var value in values)
        {
            var date = value[0] is long ? (long)value[0] : previousDate;
            previousDate = date;
            var type = (string)value[2];
            var quantity = GoogleSheetValueParser.ToDecimal(value[3]);
            var unitPrice = GoogleSheetValueParser.ToDecimal(value[5]);
            var fees = GoogleSheetValueParser.ToDecimal(value[6]) - (unitPrice * quantity);

            transactions.Add(Transaction.Create(
                DateTime.FromOADate(date),
                type == "V" ? Transaction.TransactionType.Sell : Transaction.TransactionType.Buy,
                quantity,
                unitPrice,
                fees < 0 ? 0 : fees));
        }
        return transactions;
    }

    internal async Task<List<Credit>> ReadCreditsAsync(string fileId, string spreadSheetName)
    {
        var credits = new List<Credit>();
        var values = await _service.GetSpreadSheetDataAsync(fileId, $"{spreadSheetName}!K3:N");

        if (values == null)
        {
            return credits;
        }

        foreach (var value in values)
        {
            if (value.Count > 0 && !string.IsNullOrWhiteSpace(value[0].ToString()))
            {
                var type = value.Count > 3 ? (string)value[3] : string.Empty;
                credits.Add(Credit.Create(
                    DateTime.FromOADate((long)value[0]),
                    type == "Aluguel" ? Credit.CreditType.Rent : Credit.CreditType.Dividend,
                    GoogleSheetValueParser.ToDecimal(value[1])));
            }
        }
        return credits;
    }
}
