using Financial.Application.Validation;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests;

public class OperationTypeParserTests
{
    public static IEnumerable<object?[]> NullValues => new[]
    {
        new object?[] { null }
    };

    [Theory]
    [InlineData("Buy", "Buy")]
    [InlineData("BUY", "Buy")]
    [InlineData("buy", "Buy")]
    [InlineData("Sell", "Sell")]
    [InlineData("sELL", "Sell")]
    public void TryNormalize_WhenValueMatches_ReturnsCanonicalValue(string value, string expected)
    {
        var result = OperationTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NullValues))]
    public void TryNormalize_WhenValueIsNull_ReturnsFalseAndEmpty(string? value)
    {
        var result = OperationTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Hold")]
    [InlineData("Buyy")]
    public void TryNormalize_WhenValueInvalid_ReturnsFalseAndEmpty(string value)
    {
        var result = OperationTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NullValues))]
    public void TryParse_WhenValueIsNull_ReturnsFalseAndDefault(string? value)
    {
        var result = OperationTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Operation.OperationType));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void TryParse_WhenValueIsWhitespace_ReturnsFalseAndDefault(string value)
    {
        var result = OperationTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Operation.OperationType));
    }

    [Theory]
    [InlineData("Buy", Operation.OperationType.Buy)]
    [InlineData("SELL", Operation.OperationType.Sell)]
    [InlineData(" Buy ", Operation.OperationType.Buy)]
    public void TryParse_WhenValueValid_ReturnsTrueAndParsed(string value, Operation.OperationType expected)
    {
        var result = OperationTypeParser.TryParse(value, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hold")]
    [InlineData("Dividend")]
    public void TryParse_WhenValueInvalid_ReturnsFalseAndDefault(string value)
    {
        var result = OperationTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Operation.OperationType));
    }
}
