using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
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

        json.Should().Contain("\"Version\": 3");
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
