using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class CreditCardParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = CreditCardParser.TryParse("BaAmex", out var creditCard);

        result.Should().BeTrue();
        creditCard.Should().Be(CreditCard.BaAmex);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = CreditCardParser.TryParse("NotACard", out _);

        result.Should().BeFalse();
    }
}
