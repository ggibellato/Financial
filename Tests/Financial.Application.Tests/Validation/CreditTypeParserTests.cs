using Financial.Application.Validation;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests;

public class CreditTypeParserTests
{
    public static IEnumerable<object?[]> NullValues => new[]
    {
        new object?[] { null }
    };

    [Theory]
    [InlineData("Dividend", "Dividend")]
    [InlineData("DIVIDEND", "Dividend")]
    [InlineData("dividend", "Dividend")]
    [InlineData("Rent", "Rent")]
    [InlineData("rENT", "Rent")]
    public void TryNormalize_WhenValueMatches_ReturnsCanonicalValue(string value, string expected)
    {
        var result = CreditTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NullValues))]
    public void TryNormalize_WhenValueIsNull_ReturnsFalseAndEmpty(string? value)
    {
        var result = CreditTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Rented")]
    [InlineData("Div")]
    public void TryNormalize_WhenValueInvalid_ReturnsFalseAndEmpty(string value)
    {
        var result = CreditTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NullValues))]
    public void TryParse_WhenValueIsNull_ReturnsFalseAndDefault(string? value)
    {
        var result = CreditTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Credit.CreditType));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void TryParse_WhenValueIsWhitespace_ReturnsFalseAndDefault(string value)
    {
        var result = CreditTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Credit.CreditType));
    }

    [Theory]
    [InlineData("Dividend", Credit.CreditType.Dividend)]
    [InlineData("RENT", Credit.CreditType.Rent)]
    [InlineData(" Rent ", Credit.CreditType.Rent)]
    public void TryParse_WhenValueValid_ReturnsTrueAndParsed(string value, Credit.CreditType expected)
    {
        var result = CreditTypeParser.TryParse(value, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hold")]
    [InlineData("Sell")]
    public void TryParse_WhenValueInvalid_ReturnsFalseAndDefault(string value)
    {
        var result = CreditTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Credit.CreditType));
    }
}
