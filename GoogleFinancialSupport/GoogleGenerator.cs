using Financial.Model;
using FinancialToolSupport;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GoogleFinancialSupport;

public class GoogleGenerator : IGenerator
{
    public List<string> IgnoreSpreadSheet = new List<string> {
        "Totais",
        "Totais com cotacao",
        "Recomendacoes",
        "Fundos de Investimento",
        "Opcoes"
    };

    public Dictionary<string, string> PortifolioName = new Dictionary<string, string>() {
        {"Trading 212_76a5af", "Fantastic Five Divid" },
        {"Trading 212_ffd966", "Almost Daily Dividen" },
        {"XPI_f4cccc", "Gold" },
        {"XPI_ffff", "Acoes" },
        {"XPI_cc0000", "Fundos Investimento" },
        {"XPI_222222", "Encerradas" },
        {"FreeTrade_222222", "Encerradas" },
    };

    public Dictionary<string, string> ExchangeCurrency = new Dictionary<string, string>() {
        {"Trading 212", "GBP" },
        {"XPI", "BRL" },
        {"FreeTrade", "GBP" },
    };

    private readonly GoogleService _service;
    private readonly string _path;

    public GoogleGenerator(GoogleService service, string path) {
        _service = service;
        _path = path;
    }

    public void Generate(List<string> fileNames)
    {
        var data = Investments.Create();

        foreach (var fileName in fileNames)
        {
            var exchange = Broker.Create(fileName, ExchangeCurrency[fileName]);
            data.Brokers.Add(exchange);
            var files = _service.GetFilesName();
            var file = files.FirstOrDefault(f => f.Name == fileName);

            var spreadSheets = _service.GetSpreadSheet(file.Id);
            foreach (var spreadsheet in spreadSheets)
            {
                if (IgnoreSpreadSheet.Contains(spreadsheet.Name))
                {
                    continue;
                }
                var asset = Asset.Create(spreadsheet.Name);
                var portifolioName = string.IsNullOrWhiteSpace(spreadsheet.Color) ? "Default" : spreadsheet.Color;
                if(PortifolioName.TryGetValue($"{fileName}_{portifolioName}", out string name))
                {
                    portifolioName = name;
                }

                var potifolio = exchange.AddPortifolio(portifolioName);
                potifolio.Assets.Add(asset);

                asset.Operations.AddRange(CreateOperations(file.Id, spreadsheet.Name));
                asset.Credits.AddRange(CreateCredits(file.Id, spreadsheet.Name));
                Thread.Sleep(3000);
            }
        }
        Save(data);
    }

    private void Save(Investments data)
    {
        string json = data.Serialize();
        File.WriteAllText(Path.Combine(_path, "data.json"), json);
    }

    private List<Operation> CreateOperations(string id, string spreadSheetName)
    {
        var operations = new List<Operation>();
        var values = _service.GetSpreadSheetData(id, $"{spreadSheetName}!A3:G100");
        var previousDate = 0L;

        foreach (var value in values)
        {
            var date = value[0] is long ? (long)value[0] : previousDate;
            previousDate = date;
            var type = (string)value[2];
            var quantity = ToDecimal(value[3]);
            var unitPrice = ToDecimal(value[5]);
            var fees = ToDecimal(value[6]) - (unitPrice * quantity);

            var operation = Operation.Create(
                    DateTime.FromOADate(date),
                    type == "V" ? Operation.OperationType.Sell : Operation.OperationType.Buy,
                    quantity,
                    unitPrice,
                    fees < 0 ? 0 : fees
                );
            operations.Add(operation);
        }
        return operations;
    }

    private List<Credit> CreateCredits(string id, string spreadSheetName)
    {
        var credits = new List<Credit>();
        var values = _service.GetSpreadSheetData(id, $"{spreadSheetName}!K3:N100");

        if (values == null)
        {
            return credits;
        }

        foreach (var value in values)
        {
            if(value.Count > 0 && !string.IsNullOrWhiteSpace(value[0].ToString()))
            {
                var type = value.Count > 3 ? (string)value[3] : "";
                var credit = Credit.Create(
                    DateTime.FromOADate((long)value[0]),
                    type == "Aluguel" ? Credit.CreditType.Rent : Credit.CreditType.Dividend,
                    ToDecimal(value[1])
                 );
                credits.Add(credit);
            }
        }
        return credits;
    }

    private decimal ToDecimal(object toDecimal)
    {
        if(toDecimal is ExtendedValue extendedValue && extendedValue.NumberValue != null)
        {
            return (decimal)extendedValue.NumberValue;
        }
        else
        {
            var value = toDecimal.ToString().Replace(",", "");
            return decimal.Parse(value);
        }
    }
}