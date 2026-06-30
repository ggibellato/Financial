using Financial.Domain.Entities;
using Financial.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Persistence;

public class InvestmentsJsonSerializerTests
{
    private static readonly InvestmentsSerializerAdapter Serializer = new();

    [Fact]
    public void SerializeDeserialize_RoundTripPreservesStructure()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Broker A", "USD");
        var portfolio = broker.AddPortfolio("Default");
        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));
        investments.AddBroker(broker);

        var json = Serializer.Serialize(investments);
        var result = Serializer.Deserialize(json);

        result.Should().NotBeNull();
        var brokerResult = result.Brokers.Should().ContainSingle().Which;
        var portfolioResult = brokerResult.Portfolios.Should().ContainSingle().Which;
        portfolioResult.Assets.Should().ContainSingle();
    }

    [Fact]
    public void Serialize_ProducesValidJson()
    {
        var investments = Investments.Create();
        investments.AddBroker(Broker.Create("Broker A", "BRL"));

        var json = Serializer.Serialize(investments);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("Broker A");
    }
}
