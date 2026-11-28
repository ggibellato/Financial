using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class CnpjRulesTests
{
    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11.222.333/0001-81")]
    [InlineData(" 11.222.333/0001-81 ")]
    public void Normalize_ValidDigitsOrMask_ReturnsTheFourteenDigits(string input)
    {
        CnpjRules.Normalize(input).Should().Be("11222333000181");
    }

    [Fact]
    public void Normalize_SecondValidValue_ReturnsItsDigits()
    {
        CnpjRules.Normalize("11444777000161").Should().Be("11444777000161");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Blank_ReturnsNull(string? input)
    {
        CnpjRules.Normalize(input).Should().BeNull();
    }

    [Theory]
    [InlineData("11222333000182")]
    [InlineData("11222333000191")]
    [InlineData("1122233300018")]
    [InlineData("112223330001811")]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    [InlineData("1122233300018A")]
    public void Normalize_Invalid_Throws(string input)
    {
        var act = () => CnpjRules.Normalize(input);

        act.Should().Throw<ArgumentException>().WithMessage("Enter a valid 14-digit CNPJ.");
    }
}
