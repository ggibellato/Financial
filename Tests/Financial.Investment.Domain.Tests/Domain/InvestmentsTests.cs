using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class InvestmentsTests
{
    [Fact]
    public void AddActiveBroker_AddsBrokerToActiveCollection()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Broker A", "USD");

        investments.AddActiveBroker(broker);

        investments.ActiveBrokers.Should().ContainSingle().Which.Name.Should().Be("Broker A");
        investments.HistoricBrokers.Should().BeEmpty();
    }

    [Fact]
    public void AddHistoricBroker_AddsBrokerToHistoricCollection()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Broker A", "USD");

        investments.AddHistoricBroker(broker);

        investments.HistoricBrokers.Should().ContainSingle().Which.Name.Should().Be("Broker A");
        investments.ActiveBrokers.Should().BeEmpty();
    }

    [Fact]
    public void ActiveAndHistoricBrokers_AreIndependentCollections()
    {
        var investments = Investments.Create();

        investments.AddActiveBroker(Broker.Create("Active Broker", "USD"));
        investments.AddHistoricBroker(Broker.Create("Historic Broker", "USD"));

        investments.ActiveBrokers.Should().ContainSingle().Which.Name.Should().Be("Active Broker");
        investments.HistoricBrokers.Should().ContainSingle().Which.Name.Should().Be("Historic Broker");
    }
}
