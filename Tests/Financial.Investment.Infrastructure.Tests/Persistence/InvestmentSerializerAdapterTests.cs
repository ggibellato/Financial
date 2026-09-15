using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Persistence;

public class InvestmentSerializerAdapterTests
{
    private static readonly InvestmentSerializerAdapter Serializer = new();

    [Fact]
    public void SerializeDeserialize_RoundTripPreservesActiveAndHistoricStructure()
    {
        var investments = Investments.Create();

        var activeBroker = Broker.Create("Broker A", "USD");
        var activePortfolio = activeBroker.AddPortfolio("Default");
        activePortfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));
        investments.AddActiveBroker(activeBroker);

        var historicBroker = Broker.Create("Broker B", "USD");
        var historicPortfolio = historicBroker.AddPortfolio("Uncategorized");
        historicPortfolio.AddAsset(Asset.Create("Asset B", "ISIN456", "NYSE", "BBB"));
        investments.AddHistoricBroker(historicBroker);

        var json = Serializer.Serialize(investments);
        var result = Serializer.Deserialize(json);

        result.Should().NotBeNull();

        var activeBrokerResult = result.ActiveBrokers.Should().ContainSingle().Which;
        var activePortfolioResult = activeBrokerResult.Portfolios.Should().ContainSingle().Which;
        activePortfolioResult.Assets.Should().ContainSingle().Which.Name.Should().Be("Asset A");

        var historicBrokerResult = result.HistoricBrokers.Should().ContainSingle().Which;
        var historicPortfolioResult = historicBrokerResult.Portfolios.Should().ContainSingle().Which;
        historicPortfolioResult.Assets.Should().ContainSingle().Which.Name.Should().Be("Asset B");
    }

    [Fact]
    public void SerializeDeserialize_RoundTripPreservesCurrencyAndFxRateSnapshot()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero));
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m, currency: Currency.BRL, fxRateSnapshot: snapshot));
        asset.AddCredit(Credit.Create(new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 10m, currency: Currency.BRL, fxRateSnapshot: snapshot));
        portfolio.AddAsset(asset);
        investments.AddActiveBroker(broker);

        var json = Serializer.Serialize(investments);
        var result = Serializer.Deserialize(json);

        var resultAsset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        var transaction = resultAsset.Transactions.Should().ContainSingle().Subject;
        transaction.Currency.Should().Be(Currency.BRL);
        transaction.FxRateSnapshot.Should().NotBeNull();
        transaction.FxRateSnapshot!.ToCurrency.Should().Be(Currency.GBP);
        transaction.FxRateSnapshot!.Rate.Should().Be(0.146m);
        transaction.FxRateSnapshot!.Source.Should().Be(FxRateSource.Frankfurter);
        transaction.FxRateSnapshot!.RetrievedAt.Should().Be(snapshot.RetrievedAt);

        var credit = resultAsset.Credits.Should().ContainSingle().Subject;
        credit.Currency.Should().Be(Currency.BRL);
        credit.FxRateSnapshot.Should().NotBeNull();
        credit.FxRateSnapshot!.Rate.Should().Be(0.146m);
    }

    [Fact]
    public void SerializeDeserialize_RoundTripPreservesTaxRules()
    {
        var investments = Investments.Create();
        investments.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "BR dividend withholding", "desc",
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1));
        investments.CreateTaxRule(
            Jurisdiction.UK, EventCategory.Interest, "UK interest", "desc2",
            new DateOnly(2026, 1, 1), null);

        var json = Serializer.Serialize(investments);
        var result = Serializer.Deserialize(json);

        result.TaxRules.Should().HaveCount(2);
        var bounded = result.TaxRules.Should().ContainSingle(r => r.Jurisdiction == Jurisdiction.BR).Subject;
        bounded.EventCategory.Should().Be(EventCategory.Dividend);
        bounded.Label.Should().Be("BR dividend withholding");
        bounded.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
        bounded.EffectiveTo.Should().Be(new DateOnly(2027, 1, 1));

        var openEnded = result.TaxRules.Should().ContainSingle(r => r.Jurisdiction == Jurisdiction.UK).Subject;
        openEnded.EffectiveTo.Should().BeNull();
    }

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("Broker A", "BRL"));

        var json = Serializer.Serialize(investments);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("Broker A");
    }

    [Fact]
    public void Deserialize_MissingHistoricBrokersKey_ResultsInEmptyHistoricCollection()
    {
        const string json = """
            { "ActiveBrokers": [ { "Name": "Broker A", "Currency": "USD", "Portfolios": [] } ] }
            """;

        var result = Serializer.Deserialize(json);

        result.ActiveBrokers.Should().ContainSingle();
        result.HistoricBrokers.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_MissingActiveBrokersKey_ResultsInEmptyActiveCollection()
    {
        const string json = """
            { "HistoricBrokers": [ { "Name": "Broker A", "Currency": "USD", "Portfolios": [] } ] }
            """;

        var result = Serializer.Deserialize(json);

        result.HistoricBrokers.Should().ContainSingle();
        result.ActiveBrokers.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_WritesCurrentVersion()
    {
        var json = Serializer.Serialize(Investments.Create());

        json.Should().Contain("\"Version\": 4");
    }

    [Fact]
    public void Deserialize_NoVersionProperty_TreatsAsVersion1AndRewritesLegacyRentCreditType()
    {
        var json = BuildDocumentWithCreditType("Rent", version: null);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.Type.Should().Be(Credit.CreditType.SecuritiesLendingIncome);
    }

    [Fact]
    public void Deserialize_Version1_RewritesLegacyRentCreditType()
    {
        var json = BuildDocumentWithCreditType("Rent", version: 1);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.Type.Should().Be(Credit.CreditType.SecuritiesLendingIncome);
    }

    [Fact]
    public void Deserialize_CurrentVersion_DoesNotReapplyLegacyRentMigration()
    {
        var json = BuildDocumentWithCreditType("SecuritiesLendingIncome", version: 2);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.Type.Should().Be(Credit.CreditType.SecuritiesLendingIncome);
    }

    [Theory]
    [InlineData(true, "Manual")]
    [InlineData(false, "Unknown")]
    public void Deserialize_Version2_RenamesPriceHistoryKeyAndMapsIsManualToSource(bool isManual, string expectedSource)
    {
        var json = BuildDocumentWithPriceHistory(isManual, version: 2);

        var result = Serializer.Deserialize(json);

        var asset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        asset.PriceSnapshots.Should().ContainSingle();
        var snapshot = asset.GetPriceForDate(new DateOnly(2026, 1, 1));
        snapshot.Should().NotBeNull();
        snapshot!.Price.Should().Be(100m);
        snapshot.Source.Should().Be(Enum.Parse<PriceSource>(expectedSource));
    }

    [Fact]
    public void Deserialize_NoVersionProperty_AlsoRenamesPriceHistoryKey()
    {
        var json = BuildDocumentWithPriceHistory(isManual: true, version: null);

        var result = Serializer.Deserialize(json);

        var asset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        asset.PriceSnapshots.Should().ContainSingle();
    }

    [Fact]
    public void Deserialize_CurrentVersion_DoesNotAttemptPriceHistoryRename()
    {
        const string json = """
            {
              "Version": 3,
              "ActiveBrokers": [
                {
                  "Name": "Broker A",
                  "Currency": "USD",
                  "Portfolios": [
                    {
                      "Name": "Default",
                      "Assets": [
                        {
                          "Name": "Asset A",
                          "PriceSnapshots": [
                            { "Date": "2026-01-01", "Price": 100, "Source": "Manual" }
                          ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        var result = Serializer.Deserialize(json);

        var asset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        asset.PriceSnapshots.Should().ContainSingle();
        asset.GetPriceForDate(new DateOnly(2026, 1, 1))!.IsManual.Should().BeTrue();
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-04")]
    public void Deserialize_TransactionAndCreditMissingCurrency_BackfillsFromBroker()
    {
        var json = BuildDocumentWithCurrency(transactionCurrency: null, creditCurrency: null, brokerCurrency: "BRL", version: 3);

        var result = Serializer.Deserialize(json);

        var asset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        asset.Transactions.Single().Currency.Should().Be(Currency.BRL);
        asset.Credits.Single().Currency.Should().Be(Currency.BRL);
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-06")]
    public void Deserialize_BrokerWithBlankCurrency_LeavesItsRecordsUntouched()
    {
        var json = BuildDocumentWithCurrency(transactionCurrency: null, creditCurrency: null, brokerCurrency: "", version: 3);

        var result = Serializer.Deserialize(json);

        var asset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        asset.Transactions.Single().Currency.Should().Be(default(Currency));
        asset.Credits.Single().Currency.Should().Be(default(Currency));
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-05")]
    public void Deserialize_RecordAlreadyHavingCurrency_IsNotOverwritten()
    {
        var json = BuildDocumentWithCurrency(transactionCurrency: "USD", creditCurrency: "GBP", brokerCurrency: "BRL", version: 3);

        var result = Serializer.Deserialize(json);

        var asset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        asset.Transactions.Single().Currency.Should().Be(Currency.USD);
        asset.Credits.Single().Currency.Should().Be(Currency.GBP);
    }

    private static string BuildDocumentWithCurrency(string? transactionCurrency, string? creditCurrency, string brokerCurrency, int? version)
    {
        var versionProperty = version is null ? string.Empty : $"""
            "Version": {version},
            """;
        var transactionCurrencyProperty = transactionCurrency is null ? string.Empty : $""", "Currency": "{transactionCurrency}" """;
        var creditCurrencyProperty = creditCurrency is null ? string.Empty : $""", "Currency": "{creditCurrency}" """;

        return $$"""
            {
              {{versionProperty}}
              "ActiveBrokers": [
                {
                  "Name": "Broker A",
                  "Currency": "{{brokerCurrency}}",
                  "Portfolios": [
                    {
                      "Name": "Default",
                      "Assets": [
                        {
                          "Name": "Asset A",
                          "Transactions": [
                            { "Id": "11111111-1111-1111-1111-111111111111", "Date": "2026-01-01", "Type": "Buy", "Quantity": 1, "UnitPrice": 10, "Fees": 0{{transactionCurrencyProperty}} }
                          ],
                          "Credits": [
                            { "Id": "22222222-2222-2222-2222-222222222222", "Date": "2026-01-01", "Type": "Dividend", "Value": 10, "Withheld": 0{{creditCurrencyProperty}} }
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

    private static string BuildDocumentWithPriceHistory(bool isManual, int? version)
    {
        var versionProperty = version is null ? string.Empty : $"""
            "Version": {version},
            """;

        return $$"""
            {
              {{versionProperty}}
              "ActiveBrokers": [
                {
                  "Name": "Broker A",
                  "Currency": "USD",
                  "Portfolios": [
                    {
                      "Name": "Default",
                      "Assets": [
                        {
                          "Name": "Asset A",
                          "PriceHistory": [
                            { "Date": "2026-01-01", "Price": 100, "IsManual": {{(isManual ? "true" : "false")}} }
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

    private static string BuildDocumentWithCreditType(string creditType, int? version)
    {
        var versionProperty = version is null ? string.Empty : $"""
            "Version": {version},
            """;

        return $$"""
            {
              {{versionProperty}}
              "ActiveBrokers": [
                {
                  "Name": "Broker A",
                  "Currency": "USD",
                  "Portfolios": [
                    {
                      "Name": "Default",
                      "Assets": [
                        {
                          "Name": "Asset A",
                          "Credits": [
                            { "Id": "11111111-1111-1111-1111-111111111111", "Date": "2026-01-01", "Type": "{{creditType}}", "Value": 10, "Withheld": 0 }
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
}
