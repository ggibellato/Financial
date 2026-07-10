using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Domain.Tests;

public class BrokerTests
{
    [Fact]
    public void AddPortfolio_SameName_ReturnsExisting()
    {
        var broker = Broker.Create("Broker A", "USD");

        var first = broker.AddPortfolio("Default");
        var second = broker.AddPortfolio("Default");

        first.Should().BeSameAs(second);
        broker.Portfolios.Should().ContainSingle();
    }

    [Fact]
    public void AddPortfolio_DifferentNames_AddsDistinct()
    {
        var broker = Broker.Create("Broker A", "USD");

        broker.AddPortfolio("Default");
        broker.AddPortfolio("Growth");

        broker.Portfolios.Should().HaveCount(2);
    }
}
