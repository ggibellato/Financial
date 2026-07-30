using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Integrations;

public class CountryCodeResolverTests
{
    [Fact]
    public void FromCurrency_GBP_ReturnsUnitedKingdom()
    {
        var result = CountryCodeResolver.FromCurrency("GBP");

        result.Should().Be(CountryCode.UK);
    }

    [Fact]
    public void FromCurrency_BRL_ReturnsBrazil()
    {
        var result = CountryCodeResolver.FromCurrency("BRL");

        result.Should().Be(CountryCode.BR);
    }

    [Fact]
    public void FromCurrency_USD_ReturnsUnitedStates()
    {
        var result = CountryCodeResolver.FromCurrency("USD");

        result.Should().Be(CountryCode.US);
    }

    [Fact]
    public void FromCurrency_UnrecognizedCurrency_ReturnsUnknown()
    {
        var result = CountryCodeResolver.FromCurrency("EUR");

        result.Should().Be(CountryCode.Unknown);
    }
}
