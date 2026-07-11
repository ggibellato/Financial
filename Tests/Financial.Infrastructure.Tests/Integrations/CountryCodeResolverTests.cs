using Financial.Domain.Entities;
using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Integrations;

public class CountryCodeResolverTests
{
    [Fact]
    public void FromCurrency_GBP_ReturnsUnitedKingdom()
    {
        var result = CountryCodeResolver.FromCurrency("GBP");

        result.Should().Be(CountryCode.UK);
    }
}
