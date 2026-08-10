using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class CreditCardParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        CreditCard creditCard;
        bool result = CreditCardParser.TryParse("BaAmex", out creditCard);

        result.Should().BeTrue();
        creditCard.Should().Be(CreditCard.BaAmex);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        Financial.CashFlow.Domain.Enums.CreditCard creditCard = default(Financial.CashFlow.Domain.Enums.CreditCard);
        var result = CreditCardParser.TryParse("NotACard", out creditCard);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        Financial.CashFlow.Domain.Enums.CreditCard creditCard = default(Financial.CashFlow.Domain.Enums.CreditCard);
        var result = CreditCardParser.TryParse(null, out creditCard);

        result.Should().BeFalse();
    }
}
