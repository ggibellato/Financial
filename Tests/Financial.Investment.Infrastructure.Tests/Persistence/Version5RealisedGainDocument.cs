namespace Financial.Investment.Infrastructure.Tests.Persistence;

internal static class Version5RealisedGainDocument
{
    public static readonly Guid BuyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SellId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid SupersededRecordId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid ActiveRecordId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid SupersededClassificationId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid ActiveClassificationId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public static string Build(string brokerGroup = "ActiveBrokers") => $$"""
        {
          "Version": 5,
          "{{brokerGroup}}": [
            {
              "Name": "Broker A",
              "Currency": "GBP",
              "CostBasisMethod": "FIFO",
              "Portfolios": [
                {
                  "Name": "Default",
                  "Assets": [
                    {
                      "Name": "Asset A",
                      "RealizedGainLossSharesOnly": 20,
                      "Transactions": [
                        { "Id": "{{BuyId}}", "Date": "2025-01-10", "Type": "Buy", "Quantity": 10, "UnitPrice": 10, "Fees": 0, "Currency": "GBP" },
                        { "Id": "{{SellId}}", "Date": "2025-06-10", "Type": "Sell", "Quantity": 4, "UnitPrice": 15, "Fees": 0, "Currency": "GBP" }
                      ],
                      "DisposalRecords": [
                        {
                          "Id": "{{SupersededRecordId}}",
                          "TransactionId": "{{SellId}}",
                          "Date": "2025-06-10",
                          "Method": "AverageCost",
                          "LotsConsumed": [ { "SourceTransactionId": null, "Quantity": 4, "UnitCost": 9 } ],
                          "QuantityDisposed": 4,
                          "Proceeds": 60,
                          "Currency": "GBP",
                          "TaxYear": "2025/26",
                          "Status": "Superseded",
                          "SupersededByRecordId": "{{ActiveRecordId}}",
                          "CreatedAt": "2025-06-10T09:00:00+00:00"
                        },
                        {
                          "Id": "{{ActiveRecordId}}",
                          "TransactionId": "{{SellId}}",
                          "Date": "2025-06-10",
                          "Method": "FIFO",
                          "LotsConsumed": [ { "SourceTransactionId": "{{BuyId}}", "Quantity": 4, "UnitCost": 10 } ],
                          "QuantityDisposed": 4,
                          "Proceeds": 60,
                          "Currency": "GBP",
                          "TaxYear": "2025/26",
                          "Status": "Active",
                          "SupersededByRecordId": null,
                          "CreatedAt": "2025-07-01T09:00:00+00:00"
                        }
                      ],
                      "TaxClassifications": [
                        {
                          "Id": "{{SupersededClassificationId}}",
                          "SourceType": "Disposal",
                          "SourceId": "{{SupersededRecordId}}",
                          "Jurisdiction": "UK",
                          "TaxYear": "2025/26",
                          "EventCategory": "CapitalGain",
                          "Proceeds": 60,
                          "CostBasis": 36,
                          "GainLoss": 24,
                          "CalculationStatus": "Incomplete",
                          "Status": "Superseded",
                          "SupersededByClassificationId": "{{ActiveClassificationId}}",
                          "CreatedAt": "2025-06-10T09:00:00+00:00"
                        },
                        {
                          "Id": "{{ActiveClassificationId}}",
                          "SourceType": "Disposal",
                          "SourceId": "{{ActiveRecordId}}",
                          "Jurisdiction": "UK",
                          "TaxYear": "2025/26",
                          "EventCategory": "CapitalGain",
                          "Proceeds": 60,
                          "CostBasis": 40,
                          "GainLoss": 20,
                          "CalculationStatus": "Incomplete",
                          "Status": "Active",
                          "CreatedAt": "2025-07-01T09:00:00+00:00"
                        }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;
}
