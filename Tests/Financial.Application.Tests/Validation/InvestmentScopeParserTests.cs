using Financial.Application.Enums;
using Financial.Application.Validation;
using FluentAssertions;

namespace Financial.Application.Tests;

public class InvestmentScopeParserTests
{
    public static IEnumerable<object?[]> NullValues => new[]
    {
        new object?[] { null }
    };

    [Theory]
    [MemberData(nameof(NullValues))]
    public void TryParse_WhenValueIsNull_ReturnsFalseAndDefault(string? value)
    {
        var result = InvestmentScopeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(InvestmentScope));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void TryParse_WhenValueIsWhitespace_ReturnsFalseAndDefault(string value)
    {
        var result = InvestmentScopeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(InvestmentScope));
    }

    [Theory]
    [InlineData("Active", InvestmentScope.Active)]
    [InlineData("HISTORIC", InvestmentScope.Historic)]
    [InlineData(" historic ", InvestmentScope.Historic)]
    public void TryParse_WhenValueValid_ReturnsTrueAndParsed(string value, InvestmentScope expected)
    {
        var result = InvestmentScopeParser.TryParse(value, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("closed")]
    public void TryParse_WhenValueInvalid_ReturnsFalseAndDefault(string value)
    {
        var result = InvestmentScopeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(InvestmentScope));
    }

    [Theory]
    [MemberData(nameof(NullValues))]
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
