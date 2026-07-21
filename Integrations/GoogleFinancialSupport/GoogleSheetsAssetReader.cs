using Financial.Investment.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Integrations.GoogleFinancialSupport;

internal sealed class GoogleSheetsAssetReader
{
    private const int AssetExchangeIdColumn = 0;
    private const int AssetTickerColumn = 1;
    private const int AssetIsinColumn = 2;

    private const int TransactionDateColumn = 0;
    private const int TransactionTypeColumn = 2;
    private const int TransactionQuantityColumn = 3;
    private const int TransactionUnitPriceColumn = 5;
    private const int TransactionFeesColumn = 6;
    private const string SellTransactionCode = "V";

    private const int CreditDateColumn = 0;
    private const int CreditValueColumn = 1;
    private const int CreditTypeColumn = 3;
    private const string RentCreditType = "Aluguel";

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
                exchangeId = (string)row[AssetExchangeIdColumn];
                ticker = (string)row[AssetTickerColumn];
                isin = (string)row[AssetIsinColumn];
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
            var date = value[TransactionDateColumn] is long ? (long)value[TransactionDateColumn] : previousDate;
            previousDate = date;
            var type = (string)value[TransactionTypeColumn];
            var quantity = GoogleSheetValueParser.ToDecimal(value[TransactionQuantityColumn]);
            var unitPrice = GoogleSheetValueParser.ToDecimal(value[TransactionUnitPriceColumn]);
            var fees = GoogleSheetValueParser.ToDecimal(value[TransactionFeesColumn]) - (unitPrice * quantity);

            transactions.Add(Transaction.Create(
                DateTime.FromOADate(date),
                type == SellTransactionCode ? Transaction.TransactionType.Sell : Transaction.TransactionType.Buy,
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
            if (value.Count > 0 && !string.IsNullOrWhiteSpace(value[CreditDateColumn].ToString()))
            {
                var type = value.Count > CreditTypeColumn ? (string)value[CreditTypeColumn] : string.Empty;
                credits.Add(Credit.Create(
                    DateTime.FromOADate((long)value[CreditDateColumn]),
                    type == RentCreditType ? Credit.CreditType.Rent : Credit.CreditType.Dividend,
                    GoogleSheetValueParser.ToDecimal(value[CreditValueColumn])));
            }
        }
        return credits;
    }
}
