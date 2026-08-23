using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Integrations.GoogleSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Financial.Investment.SpreadsheetImport;

internal sealed class GoogleSheetsAssetReader
{
    private const int AssetExchangeIdColumn = 0;
    private const int AssetTickerColumn = 1;
    private const int AssetIsinColumn = 2;

    private const int TransactionDateColumn = 0;
    private const int TransactionTypeColumn = 2;
    private const int TransactionQuantityColumn = 3;
    private const int TransactionUnitPriceColumn = 5;
    private const int TransactionTotalAmountColumn = 6;
    private const string SellTransactionCode = "V";

    private const int CreditDateColumn = 0;
    private const int CreditValueColumn = 1;
    private const int CreditTypeColumn = 3;
    private const string RentCreditType = "Aluguel";

    private readonly IGoogleSheetsDataSource _service;

    internal GoogleSheetsAssetReader(IGoogleSheetsDataSource service)
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

    /// <summary>
    /// <paramref name="progress"/> is the import tool's operator log. A row whose recorded total
    /// disagrees with unit price times quantity yields a negative fee, which
    /// <see cref="Transaction"/> floors to zero - a repair that used to leave no trace, so a
    /// spreadsheet with bad totals imported looking exactly like a clean one.
    /// </summary>
    internal async Task<List<Transaction>> ReadTransactionsAsync(string fileId, string spreadSheetName, IProgress<string> progress = null)
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
            var totalAmount = GoogleSheetValueParser.ToDecimal(value[TransactionTotalAmountColumn]);

            var transactionType = type == SellTransactionCode ? Transaction.TransactionType.Sell : Transaction.TransactionType.Buy;
            var transactionDate = DateTime.FromOADate(date);

            // Recovered once and handed to the entity, which floors it. Recovering it here and
            // again inside the factory left two evaluations of one rule that could disagree.
            var fees = TransactionFeeCalculator.RecoverFee(transactionType, quantity, unitPrice, totalAmount);
            if (fees < 0)
            {
                progress?.Report(
                    $"[{spreadSheetName}] {transactionDate:yyyy-MM-dd} {transactionType}: recorded total {totalAmount} "
                    + $"disagrees with {quantity} x {unitPrice}, giving a fee of {fees}. Imported with a fee of 0 - check the source row.");
            }

            transactions.Add(Transaction.Create(transactionDate, transactionType, quantity, unitPrice, fees));
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
