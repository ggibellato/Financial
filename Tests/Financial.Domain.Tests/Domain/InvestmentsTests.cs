using FluentAssertions;
using Financial.Domain.Entities;

namespace Financial.Domain.Tests;

public class InvestmentsTests
{
    [Fact]
    public void AddBroker_AddsBrokerToCollection()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Broker A", "USD");

        investments.AddBroker(broker);

        investments.Brokers.Should().ContainSingle().Which.Name.Should().Be("Broker A");
    }

    [Fact]
    public void Create_ReturnsEmptyInvestments()
    {
        var investments = Investments.Create();

        investments.Brokers.Should().BeEmpty();
    }
}
