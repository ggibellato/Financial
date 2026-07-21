using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests;

public class TransactionTypeParserTests
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
        var result = TransactionTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NullValues))]
    public void TryNormalize_WhenValueIsNull_ReturnsFalseAndEmpty(string? value)
    {
        var result = TransactionTypeParser.TryNormalize(value, out var normalized);

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
        var result = TransactionTypeParser.TryNormalize(value, out var normalized);

        result.Should().BeFalse();
        normalized.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NullValues))]
    public void TryParse_WhenValueIsNull_ReturnsFalseAndDefault(string? value)
    {
        var result = TransactionTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Transaction.TransactionType));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void TryParse_WhenValueIsWhitespace_ReturnsFalseAndDefault(string value)
    {
        var result = TransactionTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Transaction.TransactionType));
    }

    [Theory]
    [InlineData("Buy", Transaction.TransactionType.Buy)]
    [InlineData("SELL", Transaction.TransactionType.Sell)]
    [InlineData(" Buy ", Transaction.TransactionType.Buy)]
    public void TryParse_WhenValueValid_ReturnsTrueAndParsed(string value, Transaction.TransactionType expected)
    {
        var result = TransactionTypeParser.TryParse(value, out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hold")]
    [InlineData("Dividend")]
    public void TryParse_WhenValueInvalid_ReturnsFalseAndDefault(string value)
    {
        var result = TransactionTypeParser.TryParse(value, out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Transaction.TransactionType));
    }
}
