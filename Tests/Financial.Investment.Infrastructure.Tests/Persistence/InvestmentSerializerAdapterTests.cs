using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
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
    public void SerializeDeserialize_RoundTripPreservesIntermediationFee()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        asset.AddCredit(Credit.Create(new DateTime(2026, 7, 1), Credit.CreditType.SecuritiesLendingIncome, 0.16m, withheld: 0.03m, intermediationFee: 0.04m));
        portfolio.AddAsset(asset);
        investments.AddActiveBroker(broker);

        var json = Serializer.Serialize(investments);
        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Should().ContainSingle().Subject;
        credit.IntermediationFee.Should().Be(0.04m);
        credit.NetAmount.Should().Be(0.09m);
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
    public void SerializeDeserialize_RoundTripPreservesTaxClassifications()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var rule = investments.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "BR dividend", "desc", new DateOnly(2026, 1, 1), null);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 3, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2022, 1, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 10m, Currency.BRL));
        broker.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(broker);
        TaxClassificationBackfill.Apply(investments);
        var disposalClassification = asset.TaxClassifications.Single(c => c.SourceType == SourceType.Disposal);
        var creditClassification = asset.TaxClassifications.Single(c => c.SourceType == SourceType.Credit);
        creditClassification.Supersede(disposalClassification.Id);

        var json = Serializer.Serialize(investments);
        var result = Serializer.Deserialize(json);

        var resultAsset = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single();
        resultAsset.TaxClassifications.Should().HaveCount(2);

        var disposalResult = resultAsset.TaxClassifications.Should().ContainSingle(c => c.SourceType == SourceType.Disposal).Subject;
        disposalResult.Jurisdiction.Should().Be(Jurisdiction.BR);
        disposalResult.EventCategory.Should().Be(EventCategory.CapitalGain);
        disposalResult.Proceeds.Should().Be(550m);
        disposalResult.CostBasis.Should().Be(500m);
        disposalResult.GainLoss.Should().Be(50m);
        disposalResult.TaxRuleId.Should().BeNull();
        disposalResult.Status.Should().Be(TaxClassificationStatus.Active);

        var creditResult = resultAsset.TaxClassifications.Should().ContainSingle(c => c.SourceType == SourceType.Credit).Subject;
        creditResult.GrossAmount.Should().Be(100m);
        creditResult.WithheldAmount.Should().Be(10m);
        creditResult.NetAmount.Should().Be(90m);
        creditResult.TaxRuleId.Should().Be(rule.Id);
        creditResult.Status.Should().Be(TaxClassificationStatus.Superseded);
        creditResult.SupersededByClassificationId.Should().Be(disposalClassification.Id);
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

        json.Should().Contain("\"Version\": 5");
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

    [Fact]
    public void Deserialize_DocumentWithoutIntermediationFeeProperty_DefaultsToZero()
    {
        var json = BuildDocumentWithDividendCredit("Dividend", sharesForDividend: null, version: null);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.IntermediationFee.Should().Be(0m, "a credit written before this field existed has no fee recorded, and no migration backfills it");
    }

    [Fact]
    public void Deserialize_Version4_BackfillsSharesForDividendFromPositionHeldBeforeTheCreditDate()
    {
        var json = BuildDocumentWithDividendCredit("Dividend", sharesForDividend: null, version: 4);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.SharesForDividend.Should().Be(10m);
    }

    [Fact]
    public void Deserialize_Version4_BackfillsSharesForDividendForJcpCredit()
    {
        var json = BuildDocumentWithDividendCredit("JCP", sharesForDividend: null, version: 4);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.SharesForDividend.Should().Be(10m);
    }

    [Fact]
    public void Deserialize_Version4_CreditAlreadyHavingSharesForDividend_IsNotOverwritten()
    {
        var json = BuildDocumentWithDividendCredit("Dividend", sharesForDividend: 3m, version: 4);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.SharesForDividend.Should().Be(3m);
    }

    [Fact]
    public void Deserialize_CurrentVersion_DoesNotBackfillSharesForDividend()
    {
        var json = BuildDocumentWithDividendCredit("Dividend", sharesForDividend: null, version: 5);

        var result = Serializer.Deserialize(json);

        var credit = result.ActiveBrokers.Single().Portfolios.Single().Assets.Single().Credits.Single();
        credit.SharesForDividend.Should().BeNull("a credit created after the feature shipped with shares left blank means the user chose not to attribute it, not that it predates the field");
    }

    private static string BuildDocumentWithDividendCredit(string creditType, decimal? sharesForDividend, int? version)
    {
        var versionProperty = version is null ? string.Empty : $"""
            "Version": {version},
            """;
        var sharesProperty = sharesForDividend is null ? string.Empty : $""", "SharesForDividend": {sharesForDividend} """;

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
                          "Transactions": [
                            { "Id": "11111111-1111-1111-1111-111111111111", "Date": "2026-01-01", "Type": "Buy", "Quantity": 10, "UnitPrice": 10, "Fees": 0 }
                          ],
                          "Credits": [
                            { "Id": "22222222-2222-2222-2222-222222222222", "Date": "2026-02-01", "Type": "{{creditType}}", "Value": 10, "Withheld": 0{{sharesProperty}} }
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
