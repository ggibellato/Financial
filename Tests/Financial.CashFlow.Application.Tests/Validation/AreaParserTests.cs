using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Validation;

public class AreaParserTests
{
    [Fact]
    public void TryParse_ValidName_ReturnsTrueAndParsedValue()
    {
        var result = AreaParser.TryParse("Brasil", out var area);

        result.Should().BeTrue();
        area.Should().Be(Area.Brasil);
    }

    [Fact]
    public void TryParse_UnknownName_ReturnsFalse()
    {
        var result = AreaParser.TryParse("NotAnArea", out _);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_BlankValue_ReturnsFalse()
    {
        var result = AreaParser.TryParse(null, out _);

        result.Should().BeFalse();
    }
}
