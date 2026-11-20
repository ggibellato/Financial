using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests;

[Trait("Category", "Unit")]
public class TransactionTypeParserTests
{
    [Fact]
    public void TryParse_ValidValueWithSpacesAndAnyCase_ReturnsTrueAndParsed()
    {
        var result = TransactionTypeParser.TryParse(" Buy ", out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(Transaction.TransactionType.Buy);
    }

    [Fact]
    public void TryParse_UnknownValue_ReturnsFalseAndDefault()
    {
        var result = TransactionTypeParser.TryParse("Dividend", out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Transaction.TransactionType));
    }
}
