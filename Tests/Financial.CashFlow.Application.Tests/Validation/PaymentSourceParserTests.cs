using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class PaymentSourceParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = PaymentSourceParser.TryParse("Trading212", out var paymentSource);

        result.Should().BeTrue();
        paymentSource.Should().Be(PaymentSource.Trading212);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = PaymentSourceParser.TryParse("NotASource", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        var result = PaymentSourceParser.TryParse(null, out _);

        result.Should().BeFalse();
    }
}
