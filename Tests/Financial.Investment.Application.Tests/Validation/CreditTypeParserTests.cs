using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Application.Tests;

[Trait("Category", "Unit")]
public class CreditTypeParserTests
{
    [Fact]
    public void TryParse_ValidValueWithSpacesAndAnyCase_ReturnsTrueAndParsed()
    {
        var result = CreditTypeParser.TryParse(" jcp ", out var parsed);

        result.Should().BeTrue();
        parsed.Should().Be(Credit.CreditType.JCP);
    }

    [Fact]
    public void TryParse_UnknownValue_ReturnsFalseAndDefault()
    {
        var result = CreditTypeParser.TryParse("Sell", out var parsed);

        result.Should().BeFalse();
        parsed.Should().Be(default(Credit.CreditType));
    }
}
