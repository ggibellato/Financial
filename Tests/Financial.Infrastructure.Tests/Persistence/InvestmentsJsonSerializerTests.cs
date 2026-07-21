using Financial.Investment.Domain.Entities;
using Financial.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Persistence;

public class InvestmentsJsonSerializerTests
{
    private static readonly InvestmentsSerializerAdapter Serializer = new();

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
}
