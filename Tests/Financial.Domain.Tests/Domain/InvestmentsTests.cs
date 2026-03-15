using FluentAssertions;
using Financial.Domain.Entities;

namespace Financial.Domain.Tests;

public class InvestmentsTests
{
    [Fact]
    public void SerializeDeserialize_RoundTripPreservesStructure()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Broker A", "USD");
        var portfolio = broker.AddPortfolio("Default");
        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));
        investments.AddBroker(broker);

        var json = investments.Serialize();
        var result = Investments.Deserialize(json);

        result.Should().NotBeNull();
        var brokerResult = result!.Brokers.Should().ContainSingle().Which;
        var portfolioResult = brokerResult.Portfolios.Should().ContainSingle().Which;
        portfolioResult.Assets.Should().ContainSingle();
    }
}
