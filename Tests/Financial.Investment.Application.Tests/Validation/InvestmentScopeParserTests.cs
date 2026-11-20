using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Validation;
using FluentAssertions;

namespace Financial.Investment.Application.Tests;

[Trait("Category", "Unit")]
public class InvestmentScopeParserTests
{
    [Fact]
    public void TryParse_ValidValueWithSpacesAndAnyCase_ReturnsTrueAndParsed()
    {
        var result = InvestmentScopeParser.TryParse(" historic ", out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void TryParse_UnknownValue_ReturnsFalseAndDefault()
    {
        var result = InvestmentScopeParser.TryParse("closed", out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(InvestmentScope));
    }

    [Theory]
    [InlineData(null)]
    public void ParseOrDefault_WhenValueIsNull_ReturnsActive(string? value)
    {
        InvestmentScopeParser.ParseOrDefault(value).Should().Be(InvestmentScope.Active);
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    public void ParseOrDefault_WhenValueInvalid_ReturnsActive(string value)
    {
        InvestmentScopeParser.ParseOrDefault(value).Should().Be(InvestmentScope.Active);
    }

    [Fact]
    public void ParseOrDefault_WhenValueIsHistoric_ReturnsHistoric()
    {
        InvestmentScopeParser.ParseOrDefault("historic").Should().Be(InvestmentScope.Historic);
    }
}
