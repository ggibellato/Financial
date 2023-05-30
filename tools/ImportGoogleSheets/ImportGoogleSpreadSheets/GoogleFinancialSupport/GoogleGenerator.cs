using FinancialModel.Model;
using FinancialToolSupport;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace GoogleFinancialSupport
{

    public class GoogleGenerator : IGenerator
    {
        public List<string> IgnoreSpreadSheet = new List<string> {
            "Totais",
            "Totais com cotacao",
            "Recomendacoes",
            "Fundos de Investimento",
            "Opcoes"
        };

        private readonly GoogleService _service;
        private readonly string _path;

        public GoogleGenerator(GoogleService service, string path) {
            _service = service;
            _path = path;
        }

        public void Generate(List<string> fileNames)
        {
            var data = new Investments();

            foreach (var fileName in fileNames)
            {
                var exchange = new Broker(fileName);
                data.Brokers.Add(exchange);
                var potifolio = new Portifolio("Default");
                exchange.Portifolios.Add(potifolio);

                var files = _service.GetFilesName();
                var file = files.FirstOrDefault(f => f.Name == fileName);

                var spreadSheets = _service.GetSpreadSheet(file.Id);
                foreach (var spreadsheet in spreadSheets)
                {
                    if (IgnoreSpreadSheet.Contains(spreadsheet.Name))
                    {
                        continue;
                    }
                    var asset = new Asset(spreadsheet.Name);
                    potifolio.Assets.Add(asset);

                    asset.Operations.AddRange(CreateOperations(file.Id, spreadsheet.Name));
                    asset.Credits.AddRange(CreateCredits(file.Id, spreadsheet.Name));
                    Thread.Sleep(5000);
                }
            }

            Serialize(data);
        }

        private void Serialize(Investments data)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(Path.Combine(_path, "data.json"), json);
        }

        private List<Operation> CreateOperations(string id, string spreadSheetName)
        {
            var operations = new List<Operation>();
            var values = _service.GetSpreadSheetData(id, $"{spreadSheetName}!A3:G100");
            var previousDate = 0L;

            foreach (var value in values)
            {
                var operation = new Operation();

                var date = value[0] is long ? (long)value[0] : previousDate;
                operation.Date = DateTime.FromOADate(date);
                previousDate = date;
                var type = (string)value[2];
                operation.Type = type == "V" ? Operation.OperationType.Sell : Operation.OperationType.Buy;
                operation.Quantity = ToDecimal(value[3]);
                operation.UnitPrice = ToDecimal(value[5]);
                var fees = ToDecimal(value[6]) - (operation.UnitPrice * operation.Quantity);
                operation.Fees = fees < 0 ? 0 : fees;
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
                var credit = new Credit();
                if(value.Count > 0 && !string.IsNullOrWhiteSpace(value[0].ToString()))
                {
                    credit.Date = DateTime.FromOADate((long)value[0]);
                    var type = value.Count > 3 ? (string)value[3] : "";
                    credit.Type = type == "Aluguel" ? Credit.CreditType.Rent : Credit.CreditType.Dividend;
                    credit.Value = ToDecimal(value[1]);
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
}
