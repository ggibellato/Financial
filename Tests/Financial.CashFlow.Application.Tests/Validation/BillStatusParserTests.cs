using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class BillStatusParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = BillStatusParser.TryParse("Paid", out var status);

        result.Should().BeTrue();
        status.Should().Be(BillStatus.Paid);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = BillStatusParser.TryParse("NotAStatus", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        var result = BillStatusParser.TryParse(null, out _);

        result.Should().BeFalse();
    }
}
