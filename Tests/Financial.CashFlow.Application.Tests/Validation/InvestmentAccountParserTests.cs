using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class InvestmentAccountParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = InvestmentAccountParser.TryParse("ChaseSave", out var account);

        result.Should().BeTrue();
        account.Should().Be(InvestmentAccount.ChaseSave);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = InvestmentAccountParser.TryParse("NotAnAccount", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        var result = InvestmentAccountParser.TryParse(null, out _);

        result.Should().BeFalse();
    }
}
