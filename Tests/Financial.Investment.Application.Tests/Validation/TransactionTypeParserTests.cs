using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests;

[Trait("Category", "Unit")]
public class TransactionTypeParserTests
{
    public static IEnumerable<object?[]> NullValues => new[]
    {
        new object?[] { null }
    };

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
